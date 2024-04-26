using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using Parse.Domain;
using Parse.Domain.Entities;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Drawing;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Parse.Service
{
    internal class Parser
    {
        List<Domen> robotsList;

        static object consoleLock = new object();

        readonly IDbProvider dbProvider;

        public Parser(IDbProvider dbProvider)
        {
            this.dbProvider = dbProvider;
        }

        bool IsDisallow(Uri uri)
        {
            var dis = robotsList.FirstOrDefault(x => x.Host == uri.Host);
            foreach (var disallow in dis.DisallowList)
            {
                if (uri.ToString().Contains(disallow))
                {
                    return true;
                }
            }
            return false;
        }


        public async Task Parse()
        {
            await Parse(await dbProvider.GetAnotherUrls());
        }

        public async Task Parse(List<string> Urls)
        {
            robotsList = await dbProvider.GetRobots();
            var t0 = DateTime.Now;

            var semaphore = new SemaphoreSlim(16);

            var client = HttpClientFactory.Instance;

            var tasks = Urls.Select(async (url, index) =>
            {
                await semaphore.WaitAsync();
                try
                {
                    await ProcessUrlAsync(url, client);
                }
                finally
                {
                    semaphore.Release();
                }
            }).ToList();

            await Task.WhenAll(tasks);
            Console.WriteLine("Done successfully " + DateTime.Now.Subtract(t0).TotalMilliseconds);
        }

        async Task ProcessUrlAsync(string newUrl, HttpClient client)
        {
            try
            {
                if (await dbProvider.ContainsUrlandhtml(newUrl))
                {
                    await dbProvider.DeleteAnotherUrl(newUrl);
                    return;
                }

                var (left, top) = (0, 0);
                lock (consoleLock)
                {
                    (left, top) = Console.GetCursorPosition();
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.WriteLine($"Паршу {newUrl}");
                }

                var parsedUrl = await ParseUrlAsync(newUrl, client);
                if (string.IsNullOrWhiteSpace(parsedUrl.Text))
                {
                    parsedUrl.Text = "текст";
                }
                await SaveParsedUrlAsync(parsedUrl);

                lock (consoleLock)
                {
                    var n = Console.GetCursorPosition();
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.SetCursorPosition(0, top);
                    Console.WriteLine($"Паршу {newUrl}");
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.SetCursorPosition(n.Left, n.Top);
                }
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(newUrl, ex);
            }
        }

        static async Task<ParsedUrl> ParseUrlAsync(string newUrl, HttpClient client)
        {
            var parsedUrl = new ParsedUrl() { URL = newUrl };
            var html = await client.GetStringAsync(newUrl);
            Uri.TryCreate(parsedUrl.URL, new UriCreationOptions(), out Uri uri);
            parsedUrl.Title = RegexMatches.GetTitle(html);
            parsedUrl.Text = await RegexMatches.GetTextContent(html);
            parsedUrl.Links = ExtractLinks(html, uri.Host);

            return parsedUrl;
        }

        static List<string> ExtractLinks(string html, string host)
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
                catch { }
            }

            return links;
        }

        static string ExtractUrlString(string hrefValue)
        {
            var firstIndex = hrefValue.IndexOf('"') + 1;
            var lastIndex = hrefValue.LastIndexOf('"');
            var length = lastIndex - firstIndex;
            return hrefValue.Substring(firstIndex, length);
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

        async Task SaveParsedUrlAsync(ParsedUrl parsedUrl)
        {
            await dbProvider.DeleteAnotherUrl(parsedUrl.URL);
            await dbProvider.InsertParsedUrl(parsedUrl);
            await dbProvider.InsertAnotherLink(parsedUrl.Links);
        }

        async Task UpdateConsoleAsync(string url, ConsoleColor color, string message, (int, int) position)
        {
            var currentUri = new Uri(url);
            var (left, top) = Console.GetCursorPosition();
            Console.SetCursorPosition(position.Item1, position.Item2);
            Console.ForegroundColor = color;
            Console.WriteLine($"{message} {currentUri}");
            Console.ForegroundColor = ConsoleColor.White;
            Console.SetCursorPosition(left, top);
        }

        async Task HandleExceptionAsync(string url, Exception ex)
        {
            lock (consoleLock)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Ошибочка с ссылкой: {url} " + ex.Message);
                Console.ForegroundColor = ConsoleColor.White;
            }
            await dbProvider.DeleteAnotherUrl(url);
            await dbProvider.InsertUnaccessedUrl(url);
        }

    }
}
