using Parse.Domain.Entities;
using System.Text.RegularExpressions;

namespace Parse.Service
{
    /// <summary>
    /// Класс Parser отвечает за парсинг контента с сайтов.
    /// Содержит методы для извлечения ссылок, заголовка, текста и мета-тегов из HTML-кода.
    /// </summary>
    public class Parser
    {
        /// <summary>
        /// Метод ParseUrlAsync асинхронно парсит контент с указанного URL-адреса.
        /// </summary>
        /// <param name="url">URL-адрес для парсинга.</param>
        /// <param name="content">HTML-код страницы.</param>
        /// <returns>Объект ParsedUrl, содержащий извлеченные данные.</returns>
        public async Task<ParsedUrl> ParseUrlAsync(string url, string content)
        {
            // Создается экземпляр класса ParsedUrl.
            var parsedUrl = new ParsedUrl()
            {
                // URL-адрес страницы присваивается свойству URL.
                URL = url,
                // Список ссылок на странице инициализируется пустым списком.
                Links = new List<string>()
            };


            // Метод Uri.TryCreate пытается создать объект Uri на основе указанного URL-адреса.
            // Вторым параметром передаются опции создания объекта Uri.
            // Результат работы метода (объект Uri) присваивается переменной uri.
            Uri.TryCreate(parsedUrl.URL, new UriCreationOptions(), out Uri uri);

            // Извлекается заголовок страницы из HTML-кода с помощью метода GetTitle.
            parsedUrl.Title = RegexMatches.GetTitle(content);

            // Асинхронно извлекается текст страницы из HTML-кода с помощью метода GetTextContent.
            parsedUrl.Text = await RegexMatches.GetTextContent(content);

            // Извлекаются ссылки на странице из HTML-кода с помощью метода ExtractLinks.
            parsedUrl.Links = ExtractLinks(content, uri.Host);

            parsedUrl.DatePublished = DateTime.TryParse(RegexMatches.GetDatePublished(content), out DateTime date) ? date : null;

            parsedUrl.Images = RegexMatches.GetImages(content, url);

            parsedUrl.Lang = RegexMatches.GetPageLang(content);

            // Возвращается объект ParsedUrl, содержащий извлеченные данные.
            return parsedUrl;
        }

        /// <summary>
        /// Метод ExtractLinks извлекает ссылки из HTML-кода.
        /// </summary>
        /// <param name="html">HTML-код страницы.</param>
        /// <param name="host">Доменное имя сайта.</param>
        /// <returns>Список извлеченных ссылок.</returns>
        public List<string> ExtractLinks(string html, string host)
        {
            // Извлекается коллекция ссылок из HTML-кода с помощью метода GetHrefs.
            var hrefsCollection = RegexMatches.GetHrefs(html);

            // Инициализируется пустой список для хранения извлеченных ссылок.
            var links = new List<string>();

            // Перебираются все ссылки в коллекции.
            foreach (Match href in hrefsCollection)
            {
                try
                {
                    // Извлекается строка ссылки из текущего элемента коллекции.
                    var newUrlString = ExtractUrlString(href.Value);

                    // Если строка ссылки начинается с символа '/', то она преобразуется в полный URL-адрес.
                    if (newUrlString[0] == '/')
                    {
                        if (newUrlString[1] == '/')
                            // Если второй символ строки ссылки также равен '/', то префикс "https:" добавляется к строке ссылки.
                            newUrlString = "https:" + newUrlString;
                        else
                            // В противном случае, к строке ссылки добавляется доменное имя сайта.
                            newUrlString = "https://" + host + newUrlString;
                    }

                    // Если строка ссылки не является игнорируемой, то она добавляется в список извлеченных ссылок.
                    if (!IsIgnoredUrl(newUrlString))
                    {
                        if (Uri.TryCreate(newUrlString, new UriCreationOptions(), out Uri linkUri))
                        {
                            links.Add(newUrlString);
                        }
                    }
                }
                catch
                {
                    // Если возникает исключение при обработке текущей ссылки, то оно просто игнорируется.
                }
            }

            // Возвращается список извлеченных ссылок.
            return links;
        }

        /// <summary>
        /// Метод IsIgnoredUrl проверяет, является ли указанная строка игнорируемой ссылкой.
        /// </summary>
        /// <param name="url">Строка для проверки.</param>
        /// <returns>True, если строка является игнорируемой ссылкой, иначе false.</returns>
        static bool IsIgnoredUrl(string url)
        {
            // Возвращается true, если строка удовлетворяет хотя бы одному из указанных условий.
            return url.StartsWith("#") || url.StartsWith("mailto:") ||
                   url.StartsWith("whatsapp://") || url.StartsWith("viber://") ||
                   url.StartsWith("android-app") || url.Contains(".css") ||
                   url.Contains("twitter") || url.Contains("facebook") ||
                   url.StartsWith("tg://") || url.EndsWith(".woff2") ||
                   url.EndsWith(".svg") || url.EndsWith(".rss") ||
                   url.EndsWith(".png") || url.Contains("apple.com") ||
                   url.EndsWith(".jpg") || url.EndsWith(".ico") ||
                   url.EndsWith(".js") || url.EndsWith(".json") ||
                   url.EndsWith(".css") || url.EndsWith(".htm") ||
                   url.EndsWith(".htm/") || url == "0" ||
                   url.StartsWith("ui-") || url.StartsWith("vicon");
        }

        /// <summary>
        /// Метод ExtractUrlString извлекает строку ссылки из тега <a>.
        /// </summary>
        /// <param name="hrefValue">Строка, содержащая тег <a>.</param>
        /// <returns>Строка ссылки, извлеченная из тега <a>.</returns>
        string ExtractUrlString(string hrefValue)
        {
            // Извлекается индекс первого символа '" в строке.
            var firstIndex = hrefValue.IndexOf('"') + 1;

            // Извлекается индекс последнего символа '" в строке.
            var lastIndex = hrefValue.LastIndexOf('"');

            // Вычисляется длина подстроки, содержащей строку ссылки.
            var length = lastIndex - firstIndex;

            // Извлекается подстрока, содержащая строку ссылки, из исходной строки.
            return hrefValue.Substring(firstIndex, length);
        }
    }
}
