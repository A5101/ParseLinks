using Parse.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Parse.Service
{
    public class RSSReader
    {
        public async Task<List<string>> ReadRSS(string rss)
        {
            var client = HttpClientFactory.Instance;
            var rssString = await client.GetStringAsync(rss);
            var items = RegexMatches.GetItemXmlNodes(rssString);
            var links = new List<string>();
            foreach (Match item in items)
            {
                var link = RegexMatches.GetConcreteXmlNodes(item.Value, "link").Groups[1].Value;
                links.Add(link);
            }
            return links;
        }
    }
}
