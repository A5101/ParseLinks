using Parse.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Parse.Service
{
    public class Parser
    {
        public async Task<ParsedUrl> ParseUrlAsync(string url, string content)
        {
            var parsedUrl = new ParsedUrl()
            {
                URL = url,
                Links = new List<string>()
            };

            Uri.TryCreate(parsedUrl.URL, new UriCreationOptions(), out Uri uri);
            parsedUrl.Title = RegexMatches.GetTitle(content);
            parsedUrl.Text = await RegexMatches.GetTextContent(content);
            parsedUrl.Links = ExtractLinks(content, uri.Host);
            parsedUrl.Meta = RegexMatches.GetMeta(content);
            return parsedUrl;
        }


        public List<string> ExtractLinks(string html, string host)
        {
            var hrefsCollection = RegexMatches.GetHrefs(html);
            var links = new List<string>();

            foreach (Match href in hrefsCollection)
            {
                try
                {
                    var newUrlString = ExtractUrlString(href.Value);
                    if (newUrlString[0] == '/')
                    {
                        if (newUrlString[1] == '/')
                            newUrlString = "https:" + newUrlString;
                        else
                            newUrlString = "https://" + host + newUrlString;
                    }
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

                }
            }

            return links;
        }

        static bool IsIgnoredUrl(string url)
        {
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

        string ExtractUrlString(string hrefValue)
        {
            var firstIndex = hrefValue.IndexOf('"') + 1;
            var lastIndex = hrefValue.LastIndexOf('"');
            var length = lastIndex - firstIndex;
            return hrefValue.Substring(firstIndex, length);
        }
    }
}
