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

        [JsonPropertyName("category")]
        public string Category { get; set; }

        [JsonPropertyName("tags_")]
        public string Tags_ { get; set; }

        [JsonPropertyName("vector")]
        public double[] Vector { get; set; }
    }

    class Program
    {
        static Glove glove = new Glove(windowSize: 20, vectorScale: 300);

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

        static async Task DisplayInfo(int selectedMethod)
        {
            var query = "Крым сегодня";
            int textscount = 25000;
            var db = new PostgreDbProvider(connectionString);
            var t1 = DateTime.Now;

            Console.WriteLine("Получаем тексты страниц...");


            List<ParsedUrl> texts = new List<ParsedUrl>();
            List<string> list = new List<string>();
            List<double[]> textVectors = new List<double[]>();

            if (selectedMethod == 1)
            {
                list = await db.GetText();
                texts = await db.GetParsedUrlsTexts();
            }
            else if (selectedMethod == 2)
            {

                string jsonFilePath = "vectorcompilation.json";
                string jsonData = File.ReadAllText(jsonFilePath);
                List<LinkParsedJsonVector> myDataList = JsonConvert.DeserializeObject<List<LinkParsedJsonVector>>(jsonData);

                foreach (var data in myDataList)
                {
                    ParsedUrl parsedUrl = new ParsedUrl();
                    parsedUrl.URL = data.Url;
                    parsedUrl.Title = data.Title;
                    parsedUrl.Tags_ = data.Tags_;
                    if (!string.IsNullOrWhiteSpace(data.Content))
                    {
                        parsedUrl.Text = Regex.Replace(data.Content, @"<[^>]*>", " ").Replace("&quot;", "");
                        texts.Add(parsedUrl);
                        list.Add(Regex.Replace(data.Content, @"<[^>]*>", " "));
                        textVectors.Add(data.Vector);
                    }
                }
            }

            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine("Загрузка модели...");
            //await glove.Learn(texts: list.Take(textscount).ToArray(), iterations: 30);
            Console.WriteLine(t1.Subtract(DateTime.Now));
            //glove.Save();А
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

            //int iterationCount = 1;
            //foreach (var text in texts)
            //{
            //    if (iterationCount == textscount+1) break;
            //    Console.WriteLine($"Обрабатываем текст {iterationCount}");
            //    iterationCount++;
            //    var textVector = await glove.GetTextVector(text.Text);
            //    textVectors.Add(textVector);
            //}

            var queryVector = await glove.GetTextVector(query);

            var coslist = new List<(string, string, double)>();
            for (int i = 0; i < textscount; i++)
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
                        Console.WriteLine($"{decodedUrl}  {texts[listlist[i][k]].Title} {texts[listlist[i][k]].Tags_}");
                        Console.ForegroundColor = ConsoleColor.Magenta;
                    }
                    Console.WriteLine();
                }
                Console.ForegroundColor = ConsoleColor.Magenta;
                Console.WriteLine("Класстеризация завершена.");
                Console.ForegroundColor = ConsoleColor.White;
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

            //Glove glove = new Glove(windowSize: 4);
            string[] texts = {"В современном мире, недвижимость является одной из наиболее перспективных и востребованных сфер для инвестиций. " +
                "Инвестирование в недвижимость предоставляет возможность диверсификации портфеля, сохранения и увеличения капитала, а также получения стабильного и пассивного дохода в долгосрочной перспективе. " +
                "С развитием информационных технологий и интернета, веб-сервисы поиска недвижимости стали важным инструментом для инвесторов, позволяющим эффективно находить и анализировать потенциальные объекты для инвестирования.\r\n" +
                "В связи с этим, была поставлена цель ¬– в ходе прохождения научно-исследовательской работы рассмотреть теоретическую часть вопроса инвестиций в недвижимость, что даст возможность начать подготовку к разработке сервиса" +
                " поиска недвижимости для инвестиций.\r\nВыбор темы исследования обусловлен необходимостью разработки веб-сервиса поиска недвижимости для инвестиций, который бы предоставлял инвесторам удобный и надежный инструмент для" +
                " поиска и анализа недвижимости с точки зрения ее потенциала для инвестирования. Такой веб-сервис должен учитывать особенности различных регионов и видов недвижимости, предоставлять актуальную и достоверную информацию," +
                " а также обладать функционалом для проведения финансового анализа и оценки рисков.\r\nАктуальность данной темы обусловлена не только растущим интересом к инвестированию в недвижимость, но и необходимостью предоставления" +
                " инвесторам эффективных инструментов и ресурсов для принятия обоснованных решений. Современные технологии и возможности искусственного интеллекта открывают широкие перспективы для создания уникальных и инновационных" +
                " веб-сервисов, которые смогут облегчить процесс поиска и анализа объектов недвижимости для инвестиций.",
                "Практическая значимость исследования заключается в разработке и внедрении веб-сервиса, который позволит инвесторам оптимизировать процесс поиска и анализа недвижимости для инвестирования. При наличии удобного и надежного инструмента, " +
                "инвесторы смогут принимать обоснованные решения на основе актуальных данных и аналитических выводов, что в свою очередь способствует повышению эффективности инвестиций и минимизации рисков.\r\nЦелью непосредственно исследования " +
                "является теоретическая подготовка к разработке веб-сервиса поиска недвижимости для инвестиций, который предоставит инвесторам возможность эффективно находить и анализировать потенциальные объекты для инвестирования.\r\n" };
            var db = new PostgreDbProvider(connectionString);
            // var list = await db.GetText();
            //glove.Learn(texts: new string[]{ "Погода была хороша однако погода была плоха"}.ToArray(), iterations: 50);
            //glove.Save();
            List<string> urls = new List<string>()
            {
                 //   "https://www.interfax.ru/business/",
                 //   "https://www.interfax.ru/culture/",
                 //   "https://www.sport-interfax.ru/",
                 //   "https://www.interfax.ru/russia/",
                 //   //"https://www.interfax.ru/story/",
                 //   "https://www.interfax.ru/photo/",
                 //   "https://kuban.rbc.ru/",
                 "https://www.cyberforum.ru/",
                 //   "https://krasnodarmedia.su/",
                 //   "https://www.kommersant.ru/"
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
            Console.WriteLine("7. KMeans points test");
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
                        List<double[]> data = new List<double[]>
                        {
                        new double[] { 2, 2 },
                        new double[] { 3, 2 },
                        new double[] { 2, 3 },
                        new double[] { 1, 1 },
                        new double[] { 9, 9 },
                        new double[] { 8, 7 },
                        new double[] { 10,11 },
                        new double[] { 2, 10 },
                        new double[] { 2, 11 },
                        new double[] { 3, 10 },
                        new double[] { 3, 11 },

                        };
                        int k = 3;


                        // Вызываем метод Cluster вашей реализации K-средних
                        KMeans kMeans = new KMeans();
                        List<int> clusters = kMeans.Cluster(data, k);

                        // Выводим результаты
                        for (int i = 0; i < data.Count; i++)
                        {
                            Console.WriteLine("Точка ({0}, {1}) принадлежит кластеру {2}", data[i][0], data[i][1], clusters[i]);
                        }
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
