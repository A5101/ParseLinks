using Parse.Domain;
using Parse.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Parse.Service
{
    internal class Parser
    {
        List<Robots> robotsList;

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

        //bool IsAllow(Uri uri)
        //{
        //    foreach(var allow in robotsList.FirstOrDefault(x => x.Host == uri.Host).AllowList)
        //    {
        //        if (uri.ToString().Contains(allow))
        //        {
        //            return true;
        //        }
        //    }
        //    return false;
        //}

        public async Task Parse()
        {
            await Parse(await dbProvider.GetAnotherUrls());
        }

        public async Task Parse(List<string> Urls)
        {
            robotsList = await dbProvider.GetRobots();
            var t0 = DateTime.Now;

            var semaphore = new SemaphoreSlim(8);

            var client = new HttpClient() { Timeout = new TimeSpan(0, 0, 0, 8, 0) };

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
                Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

                List<string> Links = new List<string>();
                List<string> AnotherLinks = new List<string>();

                ParsedUrl urlEntity = new ParsedUrl() { URL = newUrl };

                var currentUri = new Uri(urlEntity.URL);

                var (Left, Top) = Console.GetCursorPosition();
                lock (consoleLock)
                {
                    (Left, Top) = Console.GetCursorPosition();
                    Console.WriteLine("Паршу " + currentUri.ToString());
                }

                robotsList = await dbProvider.InsertRobots(new Robots(currentUri.Host));

                //if (IsDisallow(currentUri))
                //{
                //    lock (consoleLock)
                //    {
                //        Console.ForegroundColor = ConsoleColor.Yellow;
                //        Console.WriteLine($"{currentUri} нельзя парсить");
                //        Console.ForegroundColor = ConsoleColor.White;
                //    }
                //    return;
                //}

                string html = await client.GetStringAsync(currentUri.ToString());

                urlEntity.Title = RegexMatches.GetTitle(html);
                var textContent = RegexMatches.GetTextContent(html);
                var hrefsCollection = RegexMatches.GetHrefs(html);

                foreach (Match href in hrefsCollection)
                {
                    try
                    {
                        int firstIndex = href.Value.IndexOf('"') + 1;
                        int lastIndex = href.Value.LastIndexOf('"');
                        int Lenght = lastIndex - firstIndex;
                        string newUrlString = href.Value.Substring(firstIndex, Lenght);
                        if (newUrlString[0] == '/')
                        {
                            if (newUrlString[1] == '/')
                                newUrlString = "https:" + newUrlString;
                            else
                                newUrlString = "https://" + currentUri.Host + newUrlString;
                        }
                        if (!newUrlString.StartsWith("#") && !newUrlString.StartsWith("mailto:")
                            && !newUrlString.StartsWith("whatsapp://") && !newUrlString.StartsWith("viber://")
                            && !newUrlString.StartsWith("android-app") && !newUrlString.Contains(".css")
                            && !newUrlString.Contains("twitter") && !newUrlString.Contains("facebook")
                            && !newUrlString.StartsWith("tg://") && !newUrlString.EndsWith(".woff2")
                            && !newUrlString.EndsWith(".svg") && !newUrlString.EndsWith(".rss")
                            && !newUrlString.EndsWith(".png") && !newUrlString.Contains("apple.com")
                            && !newUrlString.EndsWith(".jpg") && !newUrlString.EndsWith(".ico")
                            && !newUrlString.EndsWith(".js") && !newUrlString.EndsWith(".json")
                            && !newUrlString.EndsWith(".css") && !newUrlString.EndsWith(".htm")
                            && !newUrlString.EndsWith(".htm/") && newUrlString != "0"
                            && !newUrlString.StartsWith("ui-") && !newUrlString.StartsWith("vicon"))
                        {
                            Links.Add(newUrlString);
                            try
                            {
                                Uri linkUri = new Uri(newUrlString);
                                if (/*currentUri.Host != linkUri.Host &&*/ !AnotherLinks.Contains(newUrlString))
                                {
                                    AnotherLinks.Add(newUrlString);
                                }
                            }
                            catch
                            {
                                //lock (consoleLock)
                                //{
                                //    Console.WriteLine($"Ошибка в {newUrlString} при создании URI после парсинга");
                                //}
                            }
                        }
                        urlEntity.Links = Links;
                    }
                    catch
                    {
                        //lock (consoleLock)
                        //{
                        //    Console.WriteLine($"Ошибка в {href} при обработке после парсинга");
                        //}
                    }
                }

                urlEntity.Text = textContent;

                await dbProvider.DeleteAnotherUrl(newUrl);
                await dbProvider.InsertParsedUrl(urlEntity);
                await dbProvider.InsertAnotherLink(AnotherLinks);

                lock (consoleLock)
                {
                    var n = Console.GetCursorPosition();
                    Console.SetCursorPosition(0, Top);
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("Паршу " + currentUri.ToString());
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.SetCursorPosition(n.Left, n.Top);
                }

            }
            catch (Exception ex)
            {
                lock (consoleLock)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"Ошибочка с ссылкой: {newUrl} " + ex.Message);
                    Console.ForegroundColor = ConsoleColor.White;
                }
                await dbProvider.DeleteAnotherUrl(newUrl);
                await dbProvider.InsertUnaccessedUrl(newUrl);
            }
        }
    }
}
