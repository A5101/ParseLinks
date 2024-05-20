using Newtonsoft.Json;
using Parse.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http.Json;
using System.Reflection;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Parse.Service
{
    /// <summary>
    /// Класс RegexMatches содержит статические методы для извлечения информации из HTML-кода с помощью регулярных выражений.
    /// </summary>
    public class RegexMatches
    {
        /// <summary>
        /// Метод GetHrefs извлекает все значения атрибута href из тегов a в переданном HTML-коде.
        /// </summary>
        /// <param name="html">HTML-код, из которого будут извлечены значения атрибута href.</param>
        /// <returns>Коллекция Match, содержащая все найденные значения атрибута href.</returns>
        public static MatchCollection GetHrefs(string html)
        {
            return Regex.Matches(html, @"href=""([^""]+)""");
        }


        /// <summary>
        /// Метод GetMeta извлекает содержимое тега <head> из переданного HTML-кода.
        /// </summary>
        /// <param name="html">HTML-код, из которого будет извлечен тег <head>.</param>
        /// <returns>Строка, содержащая извлеченное содержимое тега <head>.</returns>
        public static string GetMeta(string html)
        {
            var m = Regex.Match(html, @"<head>([\s\S]*?)</head>");
            return m.Groups[1].Value;
        }

        /// <summary>
        /// Метод GetDatePublished извлекает дату публикации страницы при наличии
        /// </summary>
        /// <param name="html">HTML-код, из которого будет извлечена дата</param>
        /// <returns></returns>
        public static string GetDatePublished(string html)
        {
            var res = Regex.Match(html, @"<meta[^>]*published_time.*>");
            if (res.Success)
            {
                res = Regex.Match(res.Value, @"content=""([^""]*)""");
                return res.Groups[1].Value;
            }
            return "";
        }

        /// <summary>
        /// Метод GetImages извлекает все картинки со страницы
        /// </summary>
        /// <param name="html">HTML-код, из которого будет извлечены картинки</param>
        /// <param name="url"></param>
        /// <returns></returns>
        public static List<Image> GetImages(string html, string url)
        {
            var res = new List<Image>();

            Uri.TryCreate(url, new UriCreationOptions(), out Uri uri);

            var srcs = Regex.Matches(html, @"<img[^>]*>");

            foreach (Match match in srcs)
            {
                var src = Regex.Match(match.Value, @"src=""([^""]*)""");
                var alt = Regex.Match(match.Value, @"alt=""([^""]*)""");
                var title = Regex.Match(match.Value, @"title=""([^""]*)""");
                if (!src.Groups[1].Value.StartsWith("//"))
                {
                    res.Add(new Image()
                    {
                        Url = src.Groups[1].Value[0] == '/' ? ("https://" + uri.Host + src.Groups[1].Value) : src.Groups[1].Value,
                        SourceUrl = url,
                        Alt = alt.Success ? alt.Groups[1].Value : null,
                        Title = title.Success ? title.Groups[1].Value : null

                    });
                }
            }
            return res;

        }

        /// <summary>
        /// Позволяет получить язык страницы
        /// </summary>
        /// <param name="html"></param>
        /// <returns></returns>
        public static string GetPageLang(string html)
        {
            var lang = Regex.Match(html, @"lang=""([^""]*)""");
            return lang.Groups[1].Value;
        }

        /// <summary>
        /// Метод GetTitle извлекает заголовок страницы из переданного HTML-кода.
        /// Если заголовок не найден, то будет извлечен контент мета-тега "og_title", а затем контент мета-тега "og:title".
        /// </summary>
        /// <param name="html">HTML-код, из которого будет извлечен заголовок страницы.</param>
        /// <returns>Строка, содержащая извлеченный заголовок страницы.</returns>
        public static string GetTitle(string html)
        {
            // Объявляется переменная title для хранения извлеченного заголовка.
            var title = "";
            // Если заголовок не найден, то извлекается контент мета-тега "og:title" с помощью регулярного выражения.
            if (title == "")
            {
                title = Regex.Match(html, @"<meta\s+content=""([^""]+)""\s+name=""og:title""[^>]*>").Groups[1].Value;
            }
            // Если заголовок не найден, то извлекается контент мета-тега "og:title" с помощью регулярного выражения.
            if (title == "")
            {
                title = Regex.Match(html, @"<meta\s+name=""og:title""\s+content=""([^""]+)""[^>]*>").Groups[1].Value;
            }
            // Если заголовок не найден, то извлекается заголовок страницы с помощью регулярного выражения.
            if (title == "")
            {
                title = Regex.Match(html, @"<title>(.*?)</title>").Groups[1].Value;
            }
            // Возвращается извлеченный заголовок.
            return title;
        }

        /// <summary>
        /// Метод RemovePunctuation удаляет знаки препинания и пробелы из переданной строки.
        /// </summary>
        /// <param name="input">Строка, из которой будут удалены знаки препинания и пробелы.</param>
        /// <returns>Строка, содержащая измененный текст.</returns>
        public static string RemovePunctuation(string input)
        {
            try
            {
                return Regex.Replace(input, @"[\p{P}\p{S}\xC2\xA0]", " ").Replace("\n", "").Replace("&quot;", "");
            }
            catch
            {
                // Если возникает исключение, то выводится предупреждение и возвращается пустая строка.
                Console.WriteLine("W A R N I N G");
                Console.WriteLine(input);
                return "";
            }
        }

        /// <summary>
        /// Метод GetRssHref извлекает ссылку на RSS-ленту из переданного HTML-кода.
        /// </summary>
        /// <param name="html">HTML-код, из которого будет извлечена ссылка на RSS-ленту.</param>
        /// <returns>Строка, содержащая извлеченную ссылку на RSS-ленту.</returns>
        public static string GetRssHref(string html)
        {
            // Используется регулярное выражение для поиска ссылки на RSS-ленту в HTML-коде.
            // Результатом выполнения метода Match является объект Match, содержащий информацию о найденном совпадении.
            Match match = Regex.Match(html, @"<[^>]+\btype=""application\/rss\+xml""[^>]+\bhref=""([^""]+)""");
            // Если совпадение не найдено, то используется другое регулярное выражение для поиска ссылки.
            if (match == Match.Empty)
            {
                match = Regex.Match(html, @"<[^>]+\bhref=""([^""]+)""[^>]+\btype=""application\/rss\+xml""");
                // Если совпадение не найдено, то используется другое регулярное выражение для поиска ссылки.
                if (match == Match.Empty)
                {
                    match = Regex.Match(html, @"<[^>]*?\bhref=\""([^""]*rss[^""]*)\""[^>]*>.*?<\/[^>]*>");
                    // Если совпадение не найдено, то используется другое регулярное выражение для поиска ссылки.
                    if (match == Match.Empty)
                    {
                        // Возвращается первая группа, найденная в строке с помощью регулярного выражения.
                        return Regex.Match(html, @"(?:https?://[^""]*rss[^""]*)").Groups[0].Value;
                    }
                }
            }
            // Возвращается содержимое первой группы, найденной в строке.
            return match.Groups[1].Value;
        }

        /// <summary>
        /// Метод GetDescription извлекает описание страницы из переданного HTML-кода.
        /// Если описание не найдено, то будет извлечен контент мета-тега "og:description", а затем контент мета-тега "description".
        /// Если ни одно из значений не будет найдено, то будет возвращено слово "Title".
        /// </summary>
        /// <param name="html">HTML-код, из которого будет извлечено описание страницы.</param>
        /// <returns>Строка, содержащая извлеченное описание страницы.</returns>
        public static string GetDescription(string html)
        {
            // Объявляется переменная pattern для хранения шаблона регулярного выражения.
            string pattern = @"<meta\s+property=""og:description""\s+content=""(?<Content>[^""]*)""";
            // Используется регулярное выражение для поиска описания страницы в HTML-коде.
            // Результатом выполнения метода Match является объект Match, содержащий информацию о найденном совпадении.
            Match match = Regex.Match(html, pattern);
            // Если совпадение найдено, то извлекается содержимое группы "Content" с помощью свойства Groups.
            // Метод HtmlDecode преобразует экранированные символы в HTML-коде в их соответствующие символы Юникода.
            if (match.Success)
            {
                return WebUtility.HtmlDecode(match.Groups["Content"].Value);
            }
            // Если описание не найдено, то используется другое регулярное выражение для поиска.
            pattern = @"<meta\s+name=""description""\s+content=""(?<Content>[^""]*)""";
            match = Regex.Match(html, pattern);
            if (match.Success)
            {
                return WebUtility.HtmlDecode(match.Groups["Content"].Value);
            }
            // Если описание не найдено, то используется другое регулярное выражение для поиска.
            pattern = @"<title>(?<Content>.*?)</title>";
            match = Regex.Match(html, pattern);
            if (match.Success)
            {
                return WebUtility.HtmlDecode(match.Groups["Content"].Value);
            }
            // Если ни одно из значений не найдено, то возвращается слово "Title".
            return "Title";
        }

        /// <summary>
        /// Метод GetInnerContent извлекает содержимое указанного HTML-тега с указанным классом.
        /// </summary>
        /// <param name="html">HTML-код, из которого будет извлечен текст.</param>
        /// <param name="targetClass">Класс HTML-тега, содержимое которого будет извлечено.</param>
        /// <returns>Строка, содержащая извлеченный текст.</returns>
        static string GetInnerContent(string html, string targetClass)
        {
            // Объявляется стек tagStack для хранения тегов, которые были найдены в HTML-коде.
            Stack<string> tagStack = new Stack<string>();
            // Объявляется переменная startIndex для хранения индекса начала искомых данных в строке html.
            int startIndex = GetDivWithClass(html, targetClass);
            // Если искомые данные не найдены, то возвращается пустая строка.
            if (startIndex > 0)
            {
                // Цикл for проходит по всем символам в строке html, начиная с индекса startIndex.
                for (int i = startIndex; i < html.Length; i++)
                {
                    // Если текущий символ равен '<', то определяется, является ли этот символ началом или концом тега.
                    if (html[i] == '<')
                    {
                        // Если текущий символ равен '/', то определяется имя закрывающегося тега.
                        if (html[i + 1] == '/')
                        {
                            string tag = GetTagName(html, ref i);
                            // Если закрывающийся тег соответствует искомому тегу, то извлекаются данные из HTML-кода и возвращается строка, содержащая эти данные.
                            if (tag == "/div" && tagStack.Count == 0)
                            {
                                return html.Substring(startIndex, i - startIndex + 1);
                            }
                            // Если закрывающийся тег не соответствует искомому тегу, то он удаляется из стека tagStack.
                            else if (tag == "/div")
                            {
                                tagStack.Pop();
                            }
                        }
                        // Если текущий символ не равен '/', то определяется имя открывающегося тега.
                        else
                        {
                            string tag = GetTagName(html, ref i);
                            // Если открывающийся тег соответствует искомому тегу, то он добавляется в стек tagStack.
                            if (tag == "div")
                            {
                                tagStack.Push(tag);
                            }
                        }
                    }
                }
            }
            // Возвращается пустая строка, если искомые данные не найдены.
            return "";
        }

        /// <summary>
        /// Метод GetDivWithClass извлекает индекс начала первого HTML-тега "div" с указанным классом.
        /// </summary>
        /// <param name="html">HTML-код, из которого будет извлечен индекс.</param>
        /// <param name="targetClass">Класс HTML-тега "div", индекс начала которого будет извлечен.</param>
        /// <returns>Индекс начала первого HTML-тега "div" с указанным классом. Если такого тега нет, то возвращается -1.</returns>
        static int GetDivWithClass(string html, string targetClass)
        {
            // Объявляется переменная startIndex для хранения индекса начала искомых данных в строке html.
            int startIndex = 0;
            // Объявляется переменная endIndex для хранения индекса конца искомых данных в строке html.
            int endIndex;
            // Объявляется переменная inDiv для хранения информации о том, находимся ли мы внутри HTML-тега "div" с указанным классом.
            bool inDiv = false;
            // Используется регулярное выражение для поиска последнего HTML-тега "div" с классом "article" в строке html.
            var matches = Regex.Matches(html, @"<div[^>]*class=""[^""]*article[^""]*""[^>]*>");
            // Если такого тега нет, то возвращается -1.
            if (!matches.Any())
            {
                return -1;
            }
            // Индекс начала искомых данных устанавливается равным индексу конца последнего HTML-тега "div" с классом "article".
            startIndex = html.IndexOf(matches.Last().Value);
            // Цикл while продолжается, пока мы не находимся внутри HTML-тега "div" с указанным классом.
            while (!inDiv)
            {
                // Индекс начала следующего HTML-тега "div" в строке html устанавливается равным текущему значению переменной startIndex.
                startIndex = html.IndexOf("<div", startIndex);
                // Если такого тега нет, то возвращается -1.
                if (startIndex != -1)
                {
                    // Индекс конца следующего HTML-тега "div" в строке html устанавливается равным индексу первого символа '>' после индекса начала этого тега.
                    endIndex = html.IndexOf(">", startIndex);
                    // Извлекается строка, содержащая имя и все атрибуты следующего HTML-тега "div" в строке html.
                    var div = html[startIndex..(endIndex + 1)];
                    // Если эта строка содержит указанный класс, то мы находимся внутри искомого тега, и переменная inDiv устанавливается в true.
                    if (div.Contains(targetClass))
                    {
                        inDiv = true;
                        startIndex = endIndex;
                    }
                }
                else return -1;
            }
            // Возвращается индекс начала искомых данных в строке html.
            return startIndex;
        }

        /// <summary>
        /// Метод GetTagName извлекает имя HTML-тега из строки, содержащей этот тег.
        /// </summary>
        /// <param name="html">Строка, содержащая HTML-тег, имя которого будет извлечено.</param>
        /// <param name="currentIndex">Индекс текущего символа в строке html, с которого будет начинаться поиск имени тега.</param>
        /// <returns>Строка, содержащая имя извлеченного HTML-тега.</returns>
        static string GetTagName(string html, ref int currentIndex)
        {
            // Объявляется переменная startIndex для хранения индекса начала имени извлеченного HTML-тега в строке html.
            int startIndex = currentIndex;
            // Цикл while продолжается, пока текущий символ в строке html не равен '>'.
            while (html[currentIndex] != '>')
            {
                // Текущий символ увеличивается на 1.
                currentIndex++;
            }
            // Извлекается строка, содержащая имя и все атрибуты извлеченного HTML-тега в строке html.
            string tag = html.Substring(startIndex + 1, currentIndex - startIndex - 1).Split(' ', StringSplitOptions.RemoveEmptyEntries)[0];
            // Возвращается строка, содержащая только имя извлеченного HTML-тега.
            return tag;
        }

        /// <summary>
        /// Метод GetTextContent извлекает текстовое содержимое из HTML-кода.
        /// </summary>
        /// <param name="html">HTML-код, из которого будет извлечен текст.</param>
        /// <returns>Строка, содержащая извлеченный текст.</returns>
        public static async Task<string> GetTextContent(string html)
        {
            // Объявляется переменная article для хранения извлеченного текста.
            string article = "";
            // Извлекается текст из HTML-кода с помощью регулярного выражения.
            //article = Regex.Match(html, @"<article[^>]*>([\s\S]*?)</article>").Value;
            // Если текст не найден, то извлекается текст из HTML-кода с помощью метода GetInnerContent.
            //if (article == "")
            //{
            //    article = GetInnerContent(html, "article");
            //}
            //if (article == "")
            //{
            article = Regex.Match(html, @"<meta\s+content=""([^""]+)""\s+property=""og:description""[^>]*>").Groups[1].Value;
            article = article.Split(' ').Length < 5 ? "" : article;
            // Если текст не найден, то извлекается текст из HTML-кода с помощью регулярного выражения.
            if (article == "")
            {
                article = Regex.Match(html, @"<meta\s+property=""og:description""\s+content=""([^""]+)""[^>]*>").Groups[1].Value;
                article = article.Split(' ').Length < 5 ? "" : article;
            }
            // Если текст не найден, то извлекается текст из HTML-кода с помощью регулярного выражения.
            if (article == "")
            {
                article = Regex.Match(html, @"<meta\s+name=""description""\s+content=""([^""]+)""[^>]*>").Groups[1].Value;
            }
            // Если текст не найден, то извлекается текст из HTML-кода с помощью регулярного выражения.
            if (article == "")
            {
                article = Regex.Match(html, @"<meta\s+content=""([^""]+)""\s+name=""description""[^>]*>").Groups[1].Value;
            }
            // Если текст не найден, то извлекается заголовок страницы с помощью метода GetTitle.
            if (article == "")
            {
                article = RegexMatches.GetTitle(html);
            }
            // Извлеченный текст очищается от лишних символов с помощью метода Replace.
            //return article.Replace("&quot;", "");
            string firstString = (Regex.Replace(article, @"<script[^>]*>[\s\S]*?</script>|<style[^>]*>[\s\S]*?</style>|<.*?>", " "));
            string secondString = Regex.Replace(firstString, @"<[^>]*>", " ");
            return (Regex.Replace(secondString, @"\s+", " ").Replace("&quot;", ""));
        }

        /// <summary>
        /// Метод GetXmlNodes извлекает все узлы XML-документа из переданной строки.
        /// </summary>
        /// <param name="xml">Строка, содержащая XML-документ.</param>
        /// <returns>Коллекция Match, содержащая все найденные узлы XML-документа.</returns>
        public static MatchCollection GetXmlNodes(string xml)
        {
            // Используется регулярное выражение для поиска всех узлов XML-документа в строке.
            // Результатом выполнения метода Matches является коллекция Match, содержащая все найденные совпадения.
            return Regex.Matches(xml, @"<[^>]+>[^<]*<\/[^>]+>");
        }

        /// <summary>
        /// Метод GetItemXmlNodes извлекает все узлы "item" из переданной строки, содержащей XML-документ.
        /// </summary>
        /// <param name="xml">Строка, содержащащая XML-документ.</param>
        /// <returns>Коллекция Match, содержащащая все найденные узлы "item".</returns>
        public static MatchCollection GetItemXmlNodes(string xml)
        {
            return Regex.Matches(xml, @"<item>[\s\S]*?<\/item>");
        }

        /// <summary>
        /// Метод GetConcreteXmlNodes извлекает конкретный узел XML-документа из переданной строки.
        /// </summary>
        /// <param name="xml">Строка, содержащащая XML-документ.</param>
        /// <param name="node">Имя конкретного узла, который будет извлечен.</param>
        /// <returns>Объект Match, содержащий информацию о найденном узле XML-документа.</returns>
        public static Match GetConcreteXmlNodes(string xml, string node)
        {
            return Regex.Match(xml, @$"<{node}>([\s\S]*?)<\/{node}>");
        }
    }
}
