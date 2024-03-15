using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Parse.Service
{
    internal class RegexMatches
    {
        public static MatchCollection GetHrefs(string html)
        {
            return Regex.Matches(html, @"href=""([^""])+""");
        }

        public static string GetTitle(string html)
        {
            Match match = Regex.Match(html, @"<title>(.*?)</title>");
            if (match.Success)
            {
                return match.Groups[1].Value;
            }
            else return "Title";
        }

        public static string GetRssHref(string html)
        {
            Match match = Regex.Match(html, @"<[^>]+\btype=""application\/rss\+xml""[^>]+\bhref=""([^""]+)""");
            if (match == Match.Empty)
            {
                match = Regex.Match(html, @"<[^>]+\bhref=""([^""]+)""[^>]+\btype=""application\/rss\+xml""");
                if (match == Match.Empty)
                {
                    match = Regex.Match(html, @"<[^>]*?\bhref=\""([^""]*rss[^""]*)\""[^>]*>.*?<\/[^>]*>");
                    if (match == Match.Empty)
                    {
                        return Regex.Match(html, @"(?:https?://[^""]*rss[^""]*)").Groups[0].Value;
                    }
                }

            }
            return match.Groups[1].Value;
        }

        static string GetInnerContent(string html, string targetClass)
        {
            Stack<string> tagStack = new Stack<string>();

            int startIndex = GetDivWithClass(html, targetClass);

            if (startIndex > 0)
            {
                for (int i = startIndex; i < html.Length; i++)
                {
                    if (html[i] == '<')
                    {
                        if (html[i + 1] == '/')
                        {
                            string tag = GetTagName(html, ref i);

                            if (tag == "/div" && tagStack.Count == 0)
                            {
                                return html.Substring(startIndex, i - startIndex + 1);
                            }
                            else if (tag == "/div")
                            {
                                tagStack.Pop();
                            }
                        }
                        else
                        {
                            string tag = GetTagName(html, ref i);

                            if (tag == "div")
                            {
                                tagStack.Push(tag);
                            }
                        }
                    }
                }

            }
            return "";
        }

        static int GetDivWithClass(string html, string targetClass)
        {
            int startIndex = 0;
            int endIndex;
            bool inDiv = false;

            while (!inDiv)
            {
                startIndex = html.IndexOf("<div", startIndex);
                if (startIndex != -1)
                {
                    endIndex = html.IndexOf(">", startIndex);
                    var div = html[startIndex..endIndex];
                    if (div.Contains(targetClass))
                    {
                        inDiv = true;
                        startIndex = endIndex;
                    }
                }
                return -1;
            }
            return startIndex;
        }

        static string GetTagName(string html, ref int currentIndex)
        {
            int startIndex = currentIndex;
            while (html[currentIndex] != '>')
            {
                currentIndex++;
            }
            string tag = html.Substring(startIndex + 1, currentIndex - startIndex - 1).Split(' ', StringSplitOptions.RemoveEmptyEntries)[0];
            return tag;
        }

        public static string GetTextContent(string html)
        {
            string article = Regex.Match(html, @"<article[^>]*>([\s\S]*?)</article>").Value;
            if (article == "")
            {
                article = GetInnerContent(html, "article");
            }
            string firstString = (Regex.Replace(article, @"<script[^>]*>[\s\S]*?</script>|<style[^>]*>[\s\S]*?</style>|<.*?>", " "));
            string secondString = Regex.Replace(firstString, @"<[^>]*>", " ");
            return (Regex.Replace(secondString, @"\s+", " "));
        }

        public static MatchCollection GetXmlNodes(string xml)
        {
            return Regex.Matches(xml, @"<[^>]+>[^<]*<\/[^>]+>");
        }

        public static MatchCollection GetItemXmlNodes(string xml)
        {
            return Regex.Matches(xml, @"<item>[\s\S]*?<\/item>");
        }

        public static Match GetConcreteXmlNodes(string xml, string node)
        {
            return Regex.Match(xml, @$"<{node}>([\s\S]*?)<\/{node}>");
        }
    }
}
