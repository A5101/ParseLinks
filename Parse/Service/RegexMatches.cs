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
            MatchCollection matches = Regex.Matches(html, @"<a(.*)href=""([^""]*)""(.*)></a>");

            foreach (Match match in matches)
            {
                if (match.Groups[2].Value.Contains("rss"))
                    return match.Groups[2].Value;
            }
            return "";
        }

        public static string GetTextContent(string html)
        {
            string firstString = (Regex.Replace(html, @"<script[^>]*>[\s\S]*?</script>|<style[^>]*>[\s\S]*?</style>|<.*?>", " "));
            string secondString = Regex.Replace(firstString, @"<[^>]*>", " ");
            return (Regex.Replace(secondString, @"\s+", " "));
        }
    }
}
