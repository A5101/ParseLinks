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
    public class Crawler
    {
        List<Domen> robotsList;

        static object consoleLock = new object();

        readonly IDbProvider dbProvider;

        readonly Parser parser;

        public Crawler(IDbProvider dbProvider)
        {
            this.dbProvider = dbProvider;
            parser = new Parser();
        }


        public async Task Crawl(bool parseUniqueDomens = false)
        {
            await Crawl(await dbProvider.GetAnotherUrls(), parseUniqueDomens);
        }

        public async Task Crawl(List<string> Urls, bool parseUniqueDomens = false)
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
                
                tasks = GetTaskList(urls, parseUniqueDomens);
            }
            else
            {
                tasks = GetTaskList(Urls, parseUniqueDomens);
            }

            await Task.WhenAll(tasks);
            Console.WriteLine("Done successfully " + DateTime.Now.Subtract(t0).TotalMilliseconds);
        }

        List<Task> GetTaskList(List<string> Urls, bool parseUniqueDomens)
        {
            var semaphore = new SemaphoreSlim(16);
            var client = HttpClientFactory.Instance;

            return Urls.Select(async (url, index) =>
            {
                await semaphore.WaitAsync();
                try
                {
                    await CrawlUrlAsync(url, client, parseUniqueDomens);
                }
                finally
                {
                    semaphore.Release();
                }
            }).ToList();
        }

        async Task CrawlUrlAsync(string newUrl, HttpClient client, bool parseUniqueDomens)
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

                var html = await GetHtmlContent(newUrl, client);

                if (!string.IsNullOrWhiteSpace(html))
                {
                    var parsedUrl = await parser.ParseUrlAsync(newUrl, html);

                    if (parseUniqueDomens)
                    {
                        var domen = new Domen(newUrl);
                        await domen.SetSiteMap();                       

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
                                Console.WriteLine($"Запарсил {newUrl}");
                                Console.ForegroundColor = ConsoleColor.White;
                                Console.SetCursorPosition(n.Left, n.Top);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(newUrl, ex);
            } 
        
        }

        static async Task<string> GetHtmlContent(string newUrl, HttpClient client)
        {
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
                    return content;
                }
            }
            return "";
        }

        async Task SaveParsedUrlAsync(ParsedUrl parsedUrl)
        {
            await dbProvider.DeleteAnotherUrl(parsedUrl.URL);
            //  if (parsedUrl.URL.Replace("https://", "").Replace("http://", "").Split('/').Length > 2 )
            await dbProvider.InsertParsedUrl(parsedUrl);
            await dbProvider.InsertAnotherLink(parsedUrl.Links);
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
