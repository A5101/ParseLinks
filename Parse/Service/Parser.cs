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
using System.Threading;
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

        //bool IsDisallow(Uri uri)
        //{
        //    var dis = robotsList.FirstOrDefault(x => x.Host == uri.Host);
        //    foreach (var disallow in dis.DisallowList)
        //    {
        //        if (uri.ToString().Contains(disallow))
        //        {
        //            return true;
        //        }
        //    }
        //    return false;
        //}

        public async Task Parse(bool parseUniqueDomens = false)
        {
            await Parse(await dbProvider.GetAnotherUrls(), parseUniqueDomens);
        }

        public async Task Parse(List<string> Urls, bool parseUniqueDomens = false)
        {
            robotsList = await dbProvider.GetRobots();
            var t0 = DateTime.Now;
            List<Task> tasks = new List<Task>();

            if (parseUniqueDomens)
            {
                var urls = new List<string>();

                var domens = Urls.Select(url =>
                {
                    Uri.TryCreate(url, new UriCreationOptions(), out Uri uri);
                    return "https://" + uri.Host;
                }).Distinct();

                foreach (var domen in domens)
                {
                    if (!robotsList.Select(r => r.Host).Contains(domen))
                    {
                        urls.Add(domen);
                        // urls.AddRange(Urls.Where(url => url.Contains(domen)));
                    }
                }
                urls = urls.Distinct().ToList();
                tasks = GetTaskList(urls, parseUniqueDomens);
            }
            else
            {
                tasks = GetTaskList(Urls, parseUniqueDomens);
            }

            await Task.WhenAll(tasks);
            Console.WriteLine("Done successfully " + DateTime.Now.Subtract(t0).TotalMilliseconds);
        }

        public List<Task> GetTaskList(List<string> Urls, bool parseUniqueDomens)
        {
            var semaphore = new SemaphoreSlim(16);
            var client = HttpClientFactory.Instance;

            return Urls.Select(async (url, index) =>
            {
                await semaphore.WaitAsync();
                try
                {
                    await ProcessUrlAsync(url, client, parseUniqueDomens);
                }
                finally
                {
                    semaphore.Release();
                }
            }).ToList();
        }

        async Task ProcessUrlAsync(string newUrl, HttpClient client, bool parseUniqueDomens)
        {
            try
            {
                if (await dbProvider.ContainsUrlandhtml(newUrl))
                {
                    await dbProvider.DeleteAnotherUrl(newUrl);
                    return;
                }

                if (await dbProvider.ContainsUnaccessedUrll(newUrl))
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

                if (parseUniqueDomens)
                {
                    var domen = new Domen(newUrl) { Sitemap = new List<string>()};

                    var response = await client.GetAsync(domen.Host + "/robots.txt");
                    if (response.IsSuccessStatusCode)
                    {
                        try
                        {
                            var resp = await response.Content.ReadAsStringAsync();
                            var file = resp.Split("\n")
                                .Select(str => str.ToLower())
                            .Where(str => str.StartsWith("sitemap"))
                            .Select(str => str.Replace("sitemap: ", "").Replace("\r", "").Replace("\n", ""))
                            .Select(str =>
                            {
                            if (str.StartsWith('/'))
                                {
                                    str = domen.Host + str;
                                }
                                return str;
                            })
                            .ToList();
                            
                            domen.Sitemap = file;
                        }
                        catch
                        {

                        }
                    }

                    await dbProvider.InsertDomen(domen);
                    await dbProvider.DeleteAnotherUrl(newUrl);
                    await dbProvider.InsertAnotherLink(parsedUrl.Links);
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
                else
                {
                    if (!string.IsNullOrWhiteSpace(parsedUrl.Text))
                    {
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
                }
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(newUrl, ex);
            }
        }

        static async Task<ParsedUrl> ParseUrlAsync(string newUrl, HttpClient client)
        {
            var parsedUrl = new ParsedUrl() { URL = /*newUrl.Last() == '/' ? newUrl[..^1] : */newUrl, Links = new List<string>() };
            var request = new HttpRequestMessage(HttpMethod.Get, newUrl);
            var response = await client.SendAsync(request);

            if (response.IsSuccessStatusCode && response.StatusCode != System.Net.HttpStatusCode.NoContent)
            {
                var contentType = response.Content.Headers.ContentType;
                if (contentType != null)
                {
                    if (contentType.CharSet == null)
                    {
                        contentType.CharSet = Encoding.GetEncoding("windows-1251").WebName;
                    }

                    var content = await response.Content.ReadAsStringAsync();

                    Uri.TryCreate(parsedUrl.URL, new UriCreationOptions(), out Uri uri);
                    parsedUrl.Title = RegexMatches.GetTitle(content);
                    parsedUrl.Text = await RegexMatches.GetTextContent(content);
                    parsedUrl.Links = ExtractLinks(content, uri.Host);
                    parsedUrl.Meta = RegexMatches.GetMeta(content);
                }
            }
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
            //  if (parsedUrl.URL.Replace("https://", "").Replace("http://", "").Split('/').Length > 2 )
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
