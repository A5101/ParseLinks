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

        public static string GetTextContent(string html)
        {
            string firstString = (Regex.Replace(html, @"<script[^>]*>[\s\S]*?</script>|<style[^>]*>[\s\S]*?</style>|<.*?>", " "));
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
