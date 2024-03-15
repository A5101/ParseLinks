using Microsoft.Extensions.Configuration;
using Parse.Domain;
using Parse.Service;
using System.Text;

namespace Parse
{
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

            Glove glove = new Glove();
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
            glove.Learn(texts: texts, iterations: 50);
            glove.Save();
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

            //    List<string> list = new List<string>()
            //{
            //    "https://www.kommersant.ru/",
            //    "https://tass.ru/",
            //    "https://tass.com/",
            //    "https://www.interfax.ru/",
            //    "https://ria.ru/",
            //    "https://regnum.ru/",
            //    "https://lenta.ru/",
            //    "https://www.vedomosti.ru/",
            //    "https://rg.ru/",
            //    "https://www.kp.ru/",
            //    "https://www.mk.ru/",
            //    "https://iz.ru/",
            //    "https://www.forbes.ru/",
            //    "https://www.rbc.ru/",
            //    "https://www.gazeta.ru/",
            //    "https://vz.ru/",
            //    "https://news.ru/",
            //    "https://readovka.news/",
            //    "https://www.vesti.ru/",
            //    "https://www.1tv.ru/",
            //    "https://m24.ru/",
            //    "https://riamo.ru/",
            //    "https://www.fontanka.ru/",
            //    "https://ura.news/",
            //    "https://life.ru/",
            //    "https://mash.ru/",
            //    "https://www.rt.com/",
            //    "https://russian.rt.com/",
            //    "https://sputnikglobe.com/"
            //};

            Parser parser = new Parser(new PostgreDbProvider(connectionString));


            Console.WriteLine("1. First parse");
            Console.WriteLine("2. Another links parse");
            Console.WriteLine("3. Clear DB");
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
                        IDbProvider dbProvider = new PostgreDbProvider(connectionString);
                        await dbProvider.Truncate();
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("База данных успешно очищена.");
                        Console.ForegroundColor = ConsoleColor.White;
                        break;
                    }

            }
        }
    }
}
