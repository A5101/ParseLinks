using Microsoft.Extensions.Configuration;
using Parse.Domain;
using Parse.Domain.Entities;
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
