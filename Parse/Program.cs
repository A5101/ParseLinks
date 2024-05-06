using DeepMorphy;
using DeepMorphy.Model;
using Microsoft.Extensions.Configuration;
using Parse.Domain;
using Parse.Domain.Entities;
using Parse.Service;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Newtonsoft.Json;
using static System.Net.Mime.MediaTypeNames;
using static System.Runtime.InteropServices.JavaScript.JSType;

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
                List<LinkParsedJsonVector> myDataList = JsonConvert.DeserializeObject<List<LinkParsedJsonVector>>(jsonData);
                DirectoryInfo directory = new DirectoryInfo(lentaFilesPath);
                FileInfo[] files = directory.GetFiles("*.json");

                for (int i = 0; i < textscount; i++)
                {
                    string fileJsonData = File.ReadAllText(files[i].FullName);
                    LinkParsedJsonVector fileDataList = JsonConvert.DeserializeObject<LinkParsedJsonVector>(fileJsonData);
                    myDataList.Add(fileDataList);
                }

                foreach (var data in myDataList)
                {
                    ParsedUrl parsedUrl = new ParsedUrl();
                    parsedUrl.URL = data.Url;

                    parsedUrl.Title = data.Title != null ? data.Title : data.Headline;

                    parsedUrl.Tags_ = data.Tags_;
                    if (!string.IsNullOrWhiteSpace(data.Content))
                    {
                        parsedUrl.Text = Regex.Replace(data.Content, @"<[^>]*>", " ").Replace("&quot;", "");
                        texts.Add(parsedUrl);
                    }
                    else
                    {
                        parsedUrl.Text = data.Description;
                        if (!string.IsNullOrWhiteSpace(parsedUrl.Text))
                        {
                            texts.Add(parsedUrl);
                        }
                    }
                }
                Console.WriteLine($"Всего текстов: {texts.Count()}");
            }


            //await glove.Learn(texts: texts.Select(text => text.Text).ToArray(), iterations: 30, skipInitialize: false);
            //glove.Save();

            foreach (var text in texts)
            {
                if (!string.IsNullOrWhiteSpace(text.Text))
                {
                    textVectors.Add(await glove.GetTextVector(Regex.Replace(text.Text, @"<[^>]*>", " ").Replace("&quot;", "")));
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
            while ((s = Console.ReadLine()) != "exit") { 
            var queryVector = await glove.GetTextVector(s);

            Clusterize(texts, textVectors, queryVector);
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
                 "https://www.cyberforum.ru/",
                    "https://krasnodarmedia.su/",
                    "https://www.kommersant.ru/"
            };


            Parser parser = new Parser(new PostgreDbProvider(connectionString));
            IDbProvider dbProvider = new PostgreDbProvider(connectionString);
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("Выберите функцию:");
            Console.ForegroundColor = ConsoleColor.Yellow;

            Console.WriteLine("1. First parse");
            Console.WriteLine("2. Another links parse");
            Console.WriteLine("3. Clear DB");
            Console.WriteLine("4. Parse links with iteration");
            Console.WriteLine("5. KMeans Test");
            Console.WriteLine("6. KMeans from One JSON");
            Console.WriteLine("7. KMeans from All JSONs");
            Console.ForegroundColor = ConsoleColor.White;
            string choose = Console.ReadLine();

            switch (choose)
            {

                case "1":
                    {
                        await parser.Parse(urls);
                        break;
                    }
                case "2":
                    {
                        await parser.Parse();
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
                            await parser.Parse();
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
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine("Парсинг всех JSON файлов");
                        await DisplayInfo(2);
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
            }
        }
    }
}
