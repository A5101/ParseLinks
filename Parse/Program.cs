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

            Glove glove = new Glove(windowSize: 4);
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
            var list = await db.GetText();
            // glove.Learn(texts: list.ToArray(), iterations: 50);
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
                 "https://lenta.ru/",
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
                        Console.ForegroundColor = ConsoleColor.White;
                        Console.WriteLine("Выберите парсинга данных:");
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine("1. Парсинг с одного JSON файла");
                        Console.WriteLine("2. Парсинг с нескольких JSON файлов");
                        string choosenType = Console.ReadLine();
                        if (choosenType == "1")
                        {
                            DisplayInfo(2);
                        }
                        if (choosenType == "2")
                        {
                            DisplayInfo(2);
                        }


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


                    async Task DisplayInfo(int selectedMethod)
                    {
                        Glove glove = new Glove(windowSize: 20);
                        var db = new PostgreDbProvider(connectionString);
                        var list = await db.GetText();
                        glove.Learn(texts: list.ToArray(), iterations: 20);
                        glove.Save();
                        Console.ForegroundColor = ConsoleColor.Magenta;
                        Console.WriteLine("Загрузка модели...");
                        //Dictionary<string, double[]> wordVectors = KMeans.ReadWordVectorsFromFile("word_vectors.txt");
                        Dictionary<string, double[]> wordVectors = glove.model;
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine($"Модель загружена. Размер модели: {wordVectors.Count} слов");
                        Console.ForegroundColor = ConsoleColor.White;
                        Console.WriteLine($"--------------------------------------------");

                        Console.ForegroundColor = ConsoleColor.Magenta;
                        Console.WriteLine("Получаем тексты страниц...");
                        List<ParsedUrl> texts = new List<ParsedUrl>();
                        if (selectedMethod == 1)
                        {
                            texts = await dbProvider.GetParsedUrlsTexts();
                        }
                        else if (selectedMethod == 2)
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
                        else if (selectedMethod == 3)
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
                                Console.WriteLine($"{decodedUrl}");
                                Console.ForegroundColor = ConsoleColor.Magenta;
                            }
                            Console.WriteLine();
                            //if (clusters[i] == 1)
                            //{
                            //Console.WriteLine($"Текст {i} принадлежит кластеру {clusters[i]}.");
                            //Console.ForegroundColor = ConsoleColor.Green;
                            //string url = texts[i].URL.ToString();
                            //string decodedUrl = Uri.UnescapeDataString(url);
                            //Console.WriteLine($"{decodedUrl}");
                            //Console.ForegroundColor = ConsoleColor.Magenta;
                            // }

                        }
                        Console.ForegroundColor = ConsoleColor.Magenta;
                        Console.WriteLine("Класстеризация завершена.");
                        Console.ForegroundColor = ConsoleColor.White;
                    }
            }
        }
    }
}
