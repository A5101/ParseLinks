using Microsoft.Extensions.Configuration;
using Parse.Domain;
using Parse.Domain.Entities;
using Parse.Service;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using System.IO.Compression;
using System.Diagnostics.Metrics;

namespace Parse
{
    class LinkParsedJson
    {
        [JsonPropertyName("url")]
        public string Url { get; set; }

        [JsonPropertyName("source")]
        public string Source { get; set; }

        [JsonPropertyName("title")]
        public string Title { get; set; }

        [JsonPropertyName("date")]
        public DateTime Date { get; set; }

        [JsonPropertyName("content")]
        public string Content { get; set; }

        [JsonPropertyName("category")]
        public string Category { get; set; }

        [JsonPropertyName("tags_")]
        public string Tags_ { get; set; }
    }
    class LinkParsedJsonVector
    {
        [JsonPropertyName("url")]
        public string Url { get; set; }

        [JsonPropertyName("source")]
        public string Source { get; set; }

        [JsonPropertyName("title")]
        public string Title { get; set; }

        [JsonPropertyName("date")]
        public DateTime Date { get; set; }

        [JsonPropertyName("content")]
        public string Content { get; set; }

        // Lenta_cluster
        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("headline")]
        public string Headline { get; set; }

        [JsonPropertyName("category")]
        public string Category { get; set; }

        [JsonPropertyName("tags_")]
        public string Tags_ { get; set; }

        [JsonPropertyName("vector")]
        public double[] Vector { get; set; }
    }

    class Program
    {
        static Glove glove;

        static string connectionString = "";

        static double CosDistance(double[] vector1, double[] vector2)
        {
            double magnitude1 = Math.Sqrt(vector1.Sum(x => x * x));
            double magnitude2 = Math.Sqrt(vector2.Sum(x => x * x));

            double dotProduct = 0;
            for (int i = 0; i < vector1.Length; i++)
            {
                dotProduct += vector1[i] * vector2[i];
            }

            double cosineDistance = dotProduct / (magnitude1 * magnitude2);

            return cosineDistance;
        }

        static void Clusterize(List<ParsedUrl> texts, List<double[]> textVectors, double[] queryVector)
        {
            var coslist = new List<(string, string, double)>();
            for (int i = 0; i < texts.Count; i++)
            {
                coslist.Add((texts[i].URL, texts[i].Title, CosDistance(queryVector, textVectors[i])));

            }

            foreach (var item in coslist.OrderByDescending(l => l.Item3).Take(100))
            {
                Console.WriteLine($"{item.Item1} {item.Item2}. Сходство {item.Item3}");
            }
            string s;
            Console.WriteLine("Пожалуйста, укажите число кластеров:");
            while ((s = Console.ReadLine()) != "exit")
            {

                int clusterCount = Convert.ToInt32(s);
                KMeans kMeans = new KMeans();

                Console.WriteLine("Начинаем кластеризацию..");
                List<int> clusters = kMeans.Cluster(textVectors, clusterCount);
                Console.WriteLine("Кластеризация завершена.");
                var listlist = new List<List<int>>();
                for (int i = 0; i < clusterCount; i++)
                {
                    listlist.Add(clusters.Select((c, k) => new { Value = c, Index = k })
                                         .Where(x => x.Value == i)
                                         .Select(x => x.Index)
                                         .ToList());
                }
                for (int i = 0; i < listlist.Count; i++)
                {
                    Console.WriteLine($"Кластер {i}:");
                    for (int k = 0; k < listlist[i].Count; k++)
                    {
                        Console.WriteLine($"Текст {listlist[i][k]}");
                        Console.ForegroundColor = ConsoleColor.Green;
                        string url = texts[listlist[i][k]].URL.ToString();
                        string decodedUrl = Uri.UnescapeDataString(url);
                        Console.WriteLine($"{decodedUrl}  {texts[listlist[i][k]].Title}");
                        Console.ForegroundColor = ConsoleColor.Magenta;
                    }
                    Console.WriteLine();
                }
                Console.ForegroundColor = ConsoleColor.Magenta;
                Console.WriteLine("Класстеризация завершена.");
                Console.ForegroundColor = ConsoleColor.White;
            }
        }

        static async Task DisplayInfo(int selectedMethod)
        {
            Console.Write("Размер окна:");
            var windowSize = int.Parse(Console.ReadLine());
            Console.Write("Размер вектора:");
            var vectorScale = int.Parse(Console.ReadLine());
            glove = new Glove(windowSize: windowSize, vectorScale: vectorScale, modelPath: @"data.json");

            var query = "Paramount";
            var db = new PostgreDbProvider(connectionString);

            Console.WriteLine("Получаем тексты страниц...");


            List<ParsedUrl> texts = new List<ParsedUrl>();
            List<double[]> textVectors = new List<double[]>();

            if (selectedMethod == 1)
            {
                texts = await db.GetParsedUrlsTexts();
            }
            else if (selectedMethod == 2)
            {
                Console.Write("Количество текстов:");
                int textscount = int.Parse(Console.ReadLine());

                string jsonFilePath = "vectorcompilation.json";
                string lentaFilesPath = "lenta_cluster";
                string jsonData = File.ReadAllText(jsonFilePath);
                List<LinkParsedJsonVector> myDataList = JsonConvert.DeserializeObject<List<LinkParsedJsonVector>>(jsonData).Take(textscount).ToList();
               // DirectoryInfo directory = new DirectoryInfo(lentaFilesPath);
               // FileInfo[] files = directory.GetFiles("*.json");

                //for (int i = 0; i < textscount; i++)
                //{
                //    string fileJsonData = File.ReadAllText(files[i].FullName);
                //    LinkParsedJsonVector fileDataList = JsonConvert.DeserializeObject<LinkParsedJsonVector>(fileJsonData);
                //    myDataList.Add(fileDataList);
                //}

                foreach (var data in myDataList)
                {
                    ParsedUrl parsedUrl = new ParsedUrl();
                    parsedUrl.URL = data.Url;

                    parsedUrl.Title = data.Title != null ? data.Title : data.Headline;

                    if (!string.IsNullOrWhiteSpace(data.Content))
                    {
                        parsedUrl.Text = Regex.Replace(data.Content, @"<[^>]*>", " ").Replace("&quot;", "");
                        texts.Add(parsedUrl);
                    }
                    else
                    {
                        parsedUrl.Text = Regex.Replace(data.Description, @"<[^>]*>", " ").Replace("&quot;", "");
                        if (!string.IsNullOrWhiteSpace(parsedUrl.Text))
                        {
                            texts.Add(parsedUrl);
                        }
                    }
                }
                Console.WriteLine($"Всего текстов: {texts.Count()}");
            }

            foreach (var text in texts)
            {
                if (!string.IsNullOrWhiteSpace(text.Text))
                {
                    textVectors.Add(await glove.GetTextVector(text.Text));
                }
            }

            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine("Загрузка модели...");

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"Модель загружена. Размер модели: {glove.Model.Count} слов");
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine($"--------------------------------------------");
            Console.WriteLine("Получаем тексты..");
            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"Тексты получены. Количество текстов: {texts.Count}");
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine($"--------------------------------------------");
            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine("Начинаем класстеризацию текстов...");
            Console.WriteLine("Объединяем тексты с моделью...");
            string s = "";
            while ((s = Console.ReadLine()) != "exit")
            {
                var queryVector = await glove.GetTextVector(s);

                Clusterize(texts.ToList(), textVectors.ToList(), queryVector);
            }
        }


        public static async Task Main()
        {

            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            IConfiguration config = new ConfigurationBuilder()
           .SetBasePath(Directory.GetCurrentDirectory())
           .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
           .Build();
           
            
            connectionString = config.GetConnectionString("DefaultConnection");

            var db = new PostgreDbProvider(connectionString);
            // var list = await db.GetText();
            //glove.Learn(texts: new string[]{ "Погода была хороша однако погода была плоха"}.ToArray(), iterations: 50);
            //glove.Save();
            List<string> urls = new List<string>()
            {
                "https://www.interfax.ru/business/",
                    "https://www.interfax.ru/culture/",
                    "https://www.sport-interfax.ru/",
                    "https://www.interfax.ru/russia/",
                    "https://www.interfax.ru/story/",
                    "https://www.interfax.ru/photo/",
                    "https://kuban.rbc.ru/",
                    "https://krasnodarmedia.su/",
                    "https://www.kommersant.ru/"
            };

            Crawler crawler = new Crawler(new PostgreDbProvider(connectionString));

           var html = await Crawler.GetHtmlContent("https://www.interfax.ru/world/949476", HttpClientFactory.Instance);
            var res = RegexMatches.GetImages(html, "https://www.interfax.ru/world/949476");
            IDbProvider dbProvider = new PostgreDbProvider(connectionString);
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("Выберите функцию:");
            Console.ForegroundColor = ConsoleColor.Yellow;

            Console.WriteLine("1. First parse");
            Console.WriteLine("2. Another links parse");
            Console.WriteLine("3. Clear DB");
            Console.WriteLine("4. Parse links with iteration");
            Console.WriteLine("5. KMeans Test");
            Console.WriteLine("6. KMeans from ALL JSON");
            Console.WriteLine("7. Add JSON urls in DB");
            Console.WriteLine("9. First parse unique domens");
            Console.WriteLine("10. Another links parse unique domens");
            Console.WriteLine("11. Get all links from domens");
            Console.ForegroundColor = ConsoleColor.White;
            string choose = Console.ReadLine();

            switch (choose)
            {

                case "1":
                    {
                        Console.WriteLine("Вы выбрали 1. First parse");
                        await crawler.Crawl(urls);
                        break;
                    }
                case "2":
                    {
                        await crawler.Crawl();
                        break;
                    }
                case "3":
                    {
                        await dbProvider.Truncate();
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("База данных успешно очищена.");
                        Console.ForegroundColor = ConsoleColor.White;
                        break;
                    }
                case "4":
                    {
                        Console.WriteLine("Write iterations count:");
                        int iterationCount = Convert.ToInt32(Console.ReadLine());
                        for (int i = 0; i < iterationCount; i++)
                        {
                            await crawler.Crawl();
                            Console.ForegroundColor = ConsoleColor.Blue;
                            Console.WriteLine($"Iteration {i} ended succesfully");
                            Console.ForegroundColor = ConsoleColor.White;
                        }

                        break;
                    }
                case "5":
                    {
                        await DisplayInfo(1);

                        break;
                    }
                case "6":
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine("Парсинг с одного JSON файла");
                        await DisplayInfo(2);


                        break;
                    }
                case "7":
                    {
                        Glove glove = new Glove(modelPath: @"data.json");
                        string jsonFilePath = "vectorcompilation.json";

                        string lentaFilesPath = "lenta_cluster";
                        string jsonData = File.ReadAllText(jsonFilePath);

                        List<LinkParsedJsonVector> interfaxList = JsonConvert.DeserializeObject<List<LinkParsedJsonVector>>(jsonData);
                        List<LinkParsedJsonVector> lentaList = new List<LinkParsedJsonVector>();

                        DirectoryInfo directory = new DirectoryInfo(lentaFilesPath);
                        FileInfo[] files = directory.GetFiles("*.json");

                        for (int i = 0; i < files.Length; i++)
                        {
                            string fileJsonData = File.ReadAllText(files[i].FullName);
                            LinkParsedJsonVector fileDataList = JsonConvert.DeserializeObject<LinkParsedJsonVector>(fileJsonData);
                            lentaList.Add(fileDataList);
                        }

                        List<ParsedUrl> finalList = new List<ParsedUrl>();

                        foreach (var url in interfaxList)
                        {
                            if (!string.IsNullOrWhiteSpace(url.Content))
                            {
                                finalList.Add(new ParsedUrl()
                                {
                                    URL = url.Url,
                                    Text = url.Content,
                                    Title = url.Title,
                                    Vector = url.Vector,
                                    Links = new List<string>(),

                                }) ;
                            }

                        }

                        int count = 0;
                        foreach (var url in lentaList)
                        {
                            Console.WriteLine(++count);
                            if (!string.IsNullOrWhiteSpace(url.Description))
                            {
                                finalList.Add(new ParsedUrl()
                                {
                                    Text = url.Description,
                                    Title = url.Headline,
                                    URL = url.Url,
                                    Vector = await glove.GetTextVector(url.Description.ToLower()),
                                    Links = new List<string>(),
                                }); 
                            }
                        }
                        foreach (var parsedUrl in finalList)
                        {
                            dbProvider.InsertParsedUrl(parsedUrl);
                        }
                        Console.WriteLine("Готово");
                        break;
                    }
                case "8":
                    {
                        string jsonFilePath = "compilation.json";
                        string jsonData = File.ReadAllText(jsonFilePath);
                        List<LinkParsedJson> myDataList = System.Text.Json.JsonSerializer.Deserialize<List<LinkParsedJson>>(jsonData);
                        List<LinkParsedJsonVector> vectorData = new List<LinkParsedJsonVector>();
                        var count = 0;
                        foreach (var data in myDataList.Where(d => !string.IsNullOrWhiteSpace(d.Content)))
                        {
                            Console.WriteLine($"Обработка текста {count++}");
                            var newData = new LinkParsedJsonVector()
                            {
                                Vector = await glove.GetTextVector(data.Content),
                                Content = data.Content,
                                Url = data.Url,
                                Title = data.Title,
                                Tags_ = data.Tags_,
                                Source = data.Source,
                                Date = data.Date,
                                Category = data.Category
                            };
                            vectorData.Add(newData);
                        }
                        File.WriteAllText("vectorcompilation.json", JsonConvert.SerializeObject(vectorData));
                        break;
                    }
                case "9":
                    {
                        await crawler.Crawl(urls, parseUniqueDomens: true);
                        break;
                    }
                case "10":
                    {
                        await crawler.Crawl(parseUniqueDomens: true);
                        break;
                    }
                case "12":
                    {
                        var sites = await dbProvider.GetParsedUrlsTexts();

                        var semaphore = new SemaphoreSlim(20);
                        int count = 0;
                        var tasks = sites.Select(async (site, index) =>
                        {
                            await semaphore.WaitAsync();
                            try
                            {
                                var content = await Crawler.GetHtmlContent(site.URL, HttpClientFactory.Instance);
                                var images = RegexMatches.GetImages(content, site.URL);
                                await dbProvider.InsertImages(images);
                            }
                            catch (Exception ex)
                            {

                            }
                            finally
                            {
                                semaphore.Release();
                                Console.WriteLine($"{++count} Сохранил картинки с {site.URL}");
                            }
                        }).ToList();

                        await Task.WhenAll(tasks);

                        break;
                    }
                case "11":
                    {
                        var client = HttpClientFactory.Instance;

                        var domens = await dbProvider.GetRobots();
                        int counter = 0;

                        Console.ReadKey();
                        var semaphore = new SemaphoreSlim(100);
                        var tasks = domens.Select(async (domen, index) =>
                        {
                            await semaphore.WaitAsync();
                            try
                            {
                                var response = await client.GetAsync(domen.Host + "/robots.txt");
                                if (response.IsSuccessStatusCode)
                                {
                                    try
                                    {
                                        var resp = await response.Content.ReadAsStringAsync();
                                        var file = resp.Split("\n")
                                            .Select(str => str.ToLower())
                                        .Where(str => str.StartsWith("sitemap"))
                                        .Select(str => str.Replace("sitemap: ", "").Replace("sitemap:", "").Replace("\r", "").Replace("\n", ""))
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
                                        await dbProvider.UpdateDomen(domen);
                                        Console.WriteLine(++counter);
                                    }
                                    catch
                                    {

                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Ошибка с {domen.Host}");
                            }
                            finally
                            {
                                semaphore.Release();
                            }
                        }).ToList();

                        await Task.WhenAll(tasks);





                        domens = domens.Where(d => d.Sitemap.Count > 0).ToList();


                        var links = new List<string>(20000000);
                        counter = 0;
                      
                        int i = 0;

                        break;
                    }
            }
        }

        static Dictionary<string, List<string>> GroupUrlsByMask(List<string> urls)
        {
            Dictionary<string, List<string>> groupedUrls = new Dictionary<string, List<string>>();

            foreach (string url in urls)
            {
                string pattern = @"^(.*?\.)?([^.]+)\.([^.]+)$";
                Match match = Regex.Match(url[0..(url.IndexOf('/', 8) == -1 ? url.Length : url.IndexOf('/', 8))], pattern);

                if (match.Success && match.Groups.Count == 4)
                {
                    string mask = $"*.{match.Groups[2]}.{match.Groups[3]}";

                    if (!groupedUrls.ContainsKey(mask))
                    {
                        groupedUrls[mask] = new List<string>();
                    }

                    groupedUrls[mask].Add(url);
                }
            }

            return groupedUrls;
        }


        public static async Task<List<string>> Process(string url)
        {
            var links = new List<string>();
            var client = HttpClientFactory.Instance;
            if (url.Contains(".gz"))
            {
                var compressedStream = await client.GetStreamAsync(url);
                using GZipStream gZipStream = new GZipStream(compressedStream, CompressionMode.Decompress);
                using var streamreader = new StreamReader(gZipStream);
                var str = streamreader.ReadToEnd();
                var gzes = Regex.Matches(str, @"<loc>([^<]+)</loc>");
                foreach (Match gz in gzes)
                {
                    var resp = await Process(gz.Groups[1].Value);
                    links.AddRange(resp);
                }
            }
            else
            {
                try
                {
                    var sitemap = await client.GetStringAsync(url);
                    var semaphore = new SemaphoreSlim(10);
                    var matches = Regex.Matches(sitemap, @"<loc>([^<]+)</loc>");

                    var tasks = matches.Select(async (match, index) =>
                    {
                        await semaphore.WaitAsync();
                        try
                        {

                            try
                            {
                                var m = match.Groups[1].Value;
                                if (m.Contains(".xml"))
                                {
                                    var resp = await Process(m);
                                    links.AddRange(resp);
                                }
                                else
                                {
                                    links.Add(m);
                                }
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine();
                            }


                        }
                        finally
                        {
                            semaphore.Release();
                        }
                    }).ToList();

                    await Task.WhenAll(tasks);
                }
                catch
                {

                }
                //}
                //else
                //{
                //    foreach (Match match in matches.Take(25))
                //    {
                //        var m = match.Groups[1].Value;
                //        if (m.Contains(".xml"))
                //        {
                //            var resp = await Process(m);
                //            links.AddRange(resp);
                //        }
                //        else
                //        {
                //            links.Add(m);
                //        }
                //    }
                //    foreach (Match match in matches.Skip(matches.Count - 25).Take(25))
                //    {
                //        var m = match.Groups[1].Value;
                //        if (m.Contains(".xml"))
                //        {
                //            var resp = await Process(m);
                //            links.AddRange(resp);
                //        }
                //        else
                //        {
                //            links.Add(m);
                //        }
                //    }
                //}
            }
            //  Console.WriteLine($"Processed {url}   {links.Count}");

            return links;

        }
    }
}
