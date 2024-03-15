using Parse.Domain;
using Parse.Domain.Entities;
using System;
using System.Collections.Generic;
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

            var semaphore = new SemaphoreSlim(8);

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

                List<string> Links = new List<string>();
                List<string> AnotherLinks = new List<string>();

                ParsedUrl parsedUrl = new ParsedUrl() { URL = newUrl };

                var currentUri = new Uri(parsedUrl.URL);

                var (Left, Top) = Console.GetCursorPosition();
                lock (consoleLock)
                {
                    (Left, Top) = Console.GetCursorPosition();
                    Console.WriteLine("Паршу " + currentUri.ToString());
                }

                robotsList = await dbProvider.InsertDomen(new Domen("https://" + currentUri.Host));
                
                string html = await client.GetStringAsync(currentUri.ToString());

                parsedUrl.Title = RegexMatches.GetTitle(html);
                parsedUrl.Text = RegexMatches.GetTextContent(html);

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
                            try
                            {
                                if (Uri.TryCreate(newUrlString, new UriCreationOptions(), out Uri linkUri))
                                {
                                    if (/*currentUri.Host != linkUri.Host &&*/ !AnotherLinks.Contains(newUrlString))
                                    {
                                        AnotherLinks.Add(newUrlString);
                                        Links.Add(newUrlString);
                                    }
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
                        parsedUrl.Links = Links;

                    }
                    catch
                    {
                        //lock (consoleLock)
                        //{
                        //    Console.WriteLine($"Ошибка в {href} при обработке после парсинга");
                        //}
                    }
                }



                await dbProvider.DeleteAnotherUrl(newUrl);
                await dbProvider.InsertParsedUrl(parsedUrl);
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
