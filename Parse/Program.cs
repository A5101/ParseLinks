using Microsoft.Extensions.Configuration;
using Parse.Domain;
using Parse.Domain.Entities;
using Parse.Service;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Xml.Linq;
using static System.Net.Mime.MediaTypeNames;

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

        [JsonPropertyName("tags")]
        public List<string> Tags { get; set; }
    }

    class Program
    {
        public static async Task Main()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            IConfiguration config = new ConfigurationBuilder()
           .SetBasePath(Directory.GetCurrentDirectory())
           .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
           .Build();

            string connectionString = config.GetConnectionString("DefaultConnection");

            List<string> urls = new List<string>()
             {
                "https://www.interfax.ru/business/",
                "https://www.interfax.ru/culture/",
                "https://www.sport-interfax.ru/",
                "https://www.interfax.ru/russia/",
                "https://www.interfax.ru/story/",
                "https://www.interfax.ru/photo/",
                "https://kuban.rbc.ru/",
                "https://lenta.ru/",
                "https://krasnodarmedia.su/",
                "https://www.kommersant.ru/"
            };

            List<string> newList = new List<string>()
            {
                "https://habr.com/ru/flows/develop/articles/",
                "https://github.com/",
                "https://store.steampowered.com/",
                "https://ru.wikipedia.org/wiki/???,",
                "https://stackoverflow.com/",
                "https://music.yandex.ru/",
                "https://www.gosuslugi.ru/",
                "https://lenta.ru/"

            };
            List<string> list = new List<string>()
        {
            "https://www.kommersant.ru/",
            "https://tass.ru/",
            "https://tass.com/",
            "https://www.interfax.ru/",
            "https://ria.ru/",
            "https://regnum.ru/",
            "https://lenta.ru/",
            "https://www.vedomosti.ru/",
            "https://rg.ru/",
            "https://www.kp.ru/",
            "https://www.mk.ru/",
            "https://iz.ru/",
            "https://www.forbes.ru/",
            "https://www.rbc.ru/",
            "https://www.gazeta.ru/",
            "https://vz.ru/",
            "https://news.ru/",
            "https://readovka.news/",
            "https://www.vesti.ru/",
            "https://www.1tv.ru/",
            "https://m24.ru/",
            "https://riamo.ru/",
            "https://www.fontanka.ru/",
            "https://ura.news/",
            "https://life.ru/",
            "https://mash.ru/",
            "https://www.rt.com/",
            "https://russian.rt.com/",
            "https://sputnikglobe.com/"
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
            Console.WriteLine("6. KMeans from Json");
            string choose = Console.ReadLine();

            switch (choose)
            {

                case "1":
                    {
                        await parser.Parse(newList);
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
                        DisplayInfo(1);

                        break;
                    }
                case "6":
                    {
                        Console.ForegroundColor = ConsoleColor.White;
                        Console.WriteLine("Выберите парсинга данных:");
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine("1. Парсинг с одного JSON файла");
                        Console.WriteLine("2. Парсинг с нескольких JSON файлов");
                        string choosenType = Console.ReadLine();
                        if( choosenType == "1" )
                        {
                            DisplayInfo(2);
                        }
                        if( choosenType == "2" )
                        {
                            DisplayInfo(2);
                        }

                     
                        break;
                    }


                 async Task DisplayInfo(int selectedMethod)
                    {
                        Console.ForegroundColor = ConsoleColor.Magenta;
                        Console.WriteLine("Загрузка модели...");
                        Dictionary<string, double[]> wordVectors = KMeans.ReadWordVectorsFromFile("C:\\Users\\Kirill\\source\\repos\\ParseLinks\\Parse\\Files\\word_vectors.txt");
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine($"Модель загружена. Размер модели: {wordVectors.Count} слов");
                        Console.ForegroundColor = ConsoleColor.White;
                        Console.WriteLine($"--------------------------------------------");

                        Console.ForegroundColor = ConsoleColor.Magenta;
                        Console.WriteLine("Получаем тексты страниц...");
                        List<ParsedUrl> texts = new List<ParsedUrl>();
                        if(selectedMethod == 1)
                        {
                            texts = await dbProvider.GetParsedUrlsTexts();
                        }
                        else if(selectedMethod == 2) 
                        {
                            string jsonFilePath = "C:\\Users\\Kirill\\Desktop\\University\\Диплом\\compilation.json";
                            string jsonData = File.ReadAllText(jsonFilePath);
                            List<LinkParsedJson> myDataList = JsonSerializer.Deserialize<List<LinkParsedJson>>(jsonData);
                            texts = new List<ParsedUrl>();


                            foreach (var data in myDataList)
                            {
                                ParsedUrl parsedUrl = new ParsedUrl();
                                parsedUrl.URL = data.Url;
                                parsedUrl.Text = data.Content;
                                texts.Add(parsedUrl);
                            }
                        }
                        else if(selectedMethod == 3) 
                        {
                            string jsonFilePath = "C:\\Users\\Kirill\\Desktop\\University\\Диплом\\";
                        }
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine($"Тексты получены. Количество текстов: {texts.Count}");
                        Console.ForegroundColor = ConsoleColor.White;
                        Console.WriteLine($"--------------------------------------------");
                        Console.ForegroundColor = ConsoleColor.Magenta;
                        Console.WriteLine("Начинаем класстеризацию текстов...");
                        List<double[]> textVectors = new List<double[]>();
                        Console.WriteLine("Объединяем тексты с моделью...");
                        int iterationCount = 1;
                        foreach (ParsedUrl text in texts)
                        {
                            int matchCount = 0;
                            Console.WriteLine($"Обрабатываем текст {iterationCount}");
                            iterationCount++;
                            List<double[]> wordVectorsInText = new List<double[]>();
                            foreach (string word in text.Text.Split(' '))
                            {
                                if (wordVectors.TryGetValue(word, out double[] values))
                                {
                                    wordVectorsInText.Add(values);
                                    matchCount++;
                                }
                            }

                            if (wordVectorsInText.Any())
                            {
                                double[] averageVector = new double[wordVectorsInText.First().Length];
                                for (int i = 0; i < wordVectorsInText.First().Length; i++)
                                    averageVector[i] = wordVectorsInText.Select(w => w[i]).Average();
                                textVectors.Add(averageVector);
                            }
                            Console.WriteLine($"Всего слов в тексте: {text.Text.Split(' ').Count()}");
                            Console.WriteLine($"Найдено совпадений: {matchCount}");
                            Console.WriteLine(" ");
                        }
                        Console.WriteLine("Пожалуйста, укажите число кластеров:");
                        int clusterCount = Convert.ToInt32(Console.ReadLine());
                        KMeans kMeans = new KMeans();

                        List<int> clusters = kMeans.Cluster(textVectors, clusterCount);

                        for (int i = 0; i < clusters.Count; i++)
                        {
                            if (clusters[i] == 1)
                            {
                                Console.WriteLine($"Текст {i} принадлежит кластеру {clusters[i]}.");
                                Console.ForegroundColor = ConsoleColor.Green;
                                string url = texts[i].URL.ToString();
                                string decodedUrl = Uri.UnescapeDataString(url);
                                Console.WriteLine($"{decodedUrl}");
                                Console.ForegroundColor = ConsoleColor.Magenta;
                            }

                        }
                        Console.ForegroundColor = ConsoleColor.Magenta;
                        Console.WriteLine("Класстеризация завершена.");
                        Console.ForegroundColor = ConsoleColor.White;
                    }
            }
        }
    }
}
