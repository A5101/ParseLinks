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
    /// <summary>
    /// Класс Crawler отвечает за парсинг сайтов и сохранение полученных данных в базу данных.
    /// </summary>
    public class Crawler
    {
        // Список доменов, для которых уже были получены данные.
        List<Domen> robotsList;
        // Объект для синхронизации доступа к консоли.
        static object consoleLock = new object();
        // Объект реализующий интерфейс IDbProvider, предоставляет методы для взаимодействия с базой данных.
        readonly IDbProvider dbProvider;
        // Объект класса Parser, предоставляет методы для парсинга сайтов.
        readonly Parser parser;


        /// <summary>
        /// Конструктор класса Crawler.
        /// </summary>
        /// <param name="dbProvider">Объект реализующий интерфейс IDbProvider.</param>
        public Crawler(IDbProvider dbProvider)
        {
            // Инициализируем поле dbProvider.
            this.dbProvider = dbProvider;
            // Инициализируем поле parser.
            parser = new Parser();
        }

        /// <summary>
        /// Метод запускает парсинг сайтов.
        /// </summary>
        /// <param name="parseUniqueDomens">Флаг, указывающий на то, нужно ли парсить только уникальные домены.</param>
        public async Task Crawl(bool parseUniqueDomens = false)
        {
            // Вызываем метод Crawl, передаем ему список ссылок, полученных из базы данных и флаг parseUniqueDomens.
            await Crawl(await dbProvider.GetAnotherUrls(), parseUniqueDomens);
        }

        /// <summary>
        /// Метод запускает парсинг сайтов.
        /// </summary>
        /// <param name="Urls">Список ссылок для парсинга.</param>
        /// <param name="parseUniqueDomens">Флаг, указывающий на то, нужно ли парсить только уникальные домены.</param>
        public async Task Crawl(List<string> Urls, bool parseUniqueDomens = false)
        {
            // Инициализируем поле robotsList.
            robotsList = await dbProvider.GetRobots();
            // Запоминаем текущее время.
            var t0 = DateTime.Now;
            // Создаем список задач.
            List<Task> tasks = new List<Task>();

            // Если нужно парсить только уникальные домены, то выполняем следующий код.
            if (parseUniqueDomens)
            {
                // Создаем список ссылок.
                var urls = new List<string>();
                // Получаем список уникальных доменов из списка ссылок.
                var domens = Urls.Select(url =>
                {
                    Uri.TryCreate(url, new UriCreationOptions(), out Uri uri);
                    return "https://" + uri.Host;
                }).Distinct();
                // Перебираем каждый домен.
                foreach (var domen in domens)
                {
                    // Если домен не содержится в списке robotsList, то добавляем его ссылку в список urls.
                    if (!robotsList.Select(r => r.Host).Contains(domen))
                    {
                        urls.Add(domen);
                    }
                }
                // Получаем список задач для списка urls.
                tasks = GetTaskList(urls, parseUniqueDomens);
            }
            // Если не нужно парсить только уникальные домены, то выполняем следующий код.
            else
            {
                // Получаем список задач для списка Urls.
                tasks = GetTaskList(Urls, parseUniqueDomens);
            }
            // Выполняем все задачи из списка tasks.
            await Task.WhenAll(tasks);
            // Выводим в консоль сообщение о завершении парсинга и выводим время, затраченное на парсинг.
            Console.WriteLine("Done successfully " + DateTime.Now.Subtract(t0).TotalMilliseconds);
        }

        /// <summary>
        /// Метод возвращает список задач для парсинга сайтов.
        /// </summary>
        /// <param name="Urls">Список ссылок для парсинга.</param>
        /// <param name="parseUniqueDomens">Флаг, указывающий на то, нужно ли парсить только уникальные домены.</param>
        /// <returns>Список задач.</returns>
        List<Task> GetTaskList(List<string> Urls, bool parseUniqueDomens)
        {
            // Создаем семафор, ограничивающий количество одновременно выполняемых задач.
            var semaphore = new SemaphoreSlim(16);
            // Создаем экземпляр HttpClient для выполнения запросов к сайтам.
            var client = HttpClientFactory.Instance;
            // Преобразуем список ссылок в список задач.
            return Urls.Select(async (url, index) =>
            {
                // Ожидаем освобождения семафора.
                await semaphore.WaitAsync();
                try
                {
                    // Вызываем метод CrawlUrlAsync, передаем ему ссылку, экземпляр HttpClient и флаг parseUniqueDomens.
                    await CrawlUrlAsync(url, client, parseUniqueDomens);
                }
                finally
                {
                    // Освобождаем семафор.
                    semaphore.Release();
                }
            }).ToList();
        }

        /// <summary>
        /// Метод выполняет парсинг одной страницы сайта.
        /// </summary>
        /// <param name="newUrl">Ссылка на страницу для парсинга.</param>
        /// <param name="client">Экземпляр HttpClient для выполнения запросов к сайтам.</param>
        /// <param name="parseUniqueDomens">Флаг, указывающий на то, нужно ли парсить только уникальные домены.</param>
        async Task CrawlUrlAsync(string newUrl, HttpClient client, bool parseUniqueDomens)
        {
            try
            {
                // Проверяем, содержится ли ссылка в таблице urlandhtml.
                if (await dbProvider.ContainsUrlandhtml(newUrl))
                {
                    // Если содержится, то удаляем ее из таблицы anotherurls и возвращаемся из метода.
                    await dbProvider.DeleteAnotherUrl(newUrl);
                    return;
                }
                // Проверяем, содержится ли ссылка в таблице unaccessedurl.
                if (await dbProvider.ContainsUnaccessedUrll(newUrl))
                {
                    // Если содержится, то удаляем ее из таблицы anotherurls и возвращаемся из метода.
                    await dbProvider.DeleteAnotherUrl(newUrl);
                    return;
                }
                // Запоминаем текущую позицию курсора в консоли.
                var (left, top) = (0, 0);
                lock (consoleLock)
                {
                    (left, top) = Console.GetCursorPosition();
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.WriteLine($"Паршу {newUrl}");
                }
                // Получаем HTML-код страницы.
                var html = await GetHtmlContent(newUrl, client);
                // Проверяем, что HTML-код не пустой.
                if (!string.IsNullOrWhiteSpace(html))
                {
                    // Парсим HTML-код страницы.
                    var parsedUrl = await parser.ParseUrlAsync(newUrl, html);
                    // Если нужно парсить только уникальные домены, то выполняем следующий код.
                    if (parseUniqueDomens)
                    {
                        // Создаем экземпляр класса Domen для текущей страницы.
                        var domen = new Domen(newUrl);
                        // Получаем карту сайта для текущего домена.
                        await domen.SetSiteMap();
                        // Сохраняем информацию о текущем домене в базу данных.
                        await dbProvider.InsertDomen(domen);
                        // Удаляем текущую ссылку из таблицы anotherurls.
                        await dbProvider.DeleteAnotherUrl(newUrl);
                        // Сохраняем ссылки на подстраницы текущей страницы в таблицу anotherurls.
                        await dbProvider.InsertAnotherLink(parsedUrl.Links);
                        // Выводим в консоль сообщение о завершении парсинга текущей страницы.
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
                    // Если не нужно парсить только уникальные домены, то выполняем следующий код.
                    else
                    {
                        // Проверяем, что текст страницы не пустой.
                        if (!string.IsNullOrWhiteSpace(parsedUrl.Text))
                        {
                            // Сохраняем информацию о текущей странице в базу данных.
                            await SaveParsedUrlAsync(parsedUrl);
                            // Выводим в консоль сообщение о завершении парсинга текущей страницы.
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
                // Если возникло исключение, то вызываем метод HandleExceptionAsync, передаем ему ссылку на текущую страницу и информацию об исключении.
                await HandleExceptionAsync(newUrl, ex);
            }
        }

        /// <summary>
        /// Метод возвращает HTML-код страницы.
        /// </summary>
        /// <param name="newUrl">Ссылка на страницу для парсинга.</param>
        /// <param name="client">Экземпляр HttpClient для выполнения запросов к сайтам.</param>
        /// <returns>HTML-код страницы.</returns>
        public static async Task<string> GetHtmlContent(string newUrl, HttpClient client)
        {
            // Создаем запрос на получение HTML-кода страницы.
            var request = new HttpRequestMessage(HttpMethod.Get, newUrl);
            // Выполняем запрос и получаем ответ.
            var response = await client.SendAsync(request);
            // Проверяем, что ответ содержит HTML-код.
            if (response.IsSuccessStatusCode && response.StatusCode != System.Net.HttpStatusCode.NoContent)
            {
                // Получаем информацию о кодировке текста.
                var contentType = response.Content.Headers.ContentType;
                if (contentType != null)
                {
                    // Если информация о кодировке отсутствует, то используем кодировку windows-1251.
                    if (contentType.CharSet == null)
                    {
                        contentType.CharSet = Encoding.GetEncoding("windows-1251").WebName;
                    }
                    // Получаем HTML-код страницы.
                    var content = await response.Content.ReadAsStringAsync();
                    return content;
                }
            }
            // Возвращаем пустую строку, если HTML-код не получен.
            return "";
        }

        /// <summary>
        /// Метод сохраняет информацию о странице в базу данных.
        /// </summary>
        /// <param name="parsedUrl">Информация о странице.</param>
        async Task SaveParsedUrlAsync(ParsedUrl parsedUrl)
        {
            // Удаляем текущую ссылку из таблицы anotherurls.
            await dbProvider.DeleteAnotherUrl(parsedUrl.URL);
            // Сохраняем информацию о странице в таблице urlandhtml.
          // await dbProvider.InsertParsedUrl(parsedUrl);
            // Сохраняем ссылки на подстраницы текущей страницы в таблицу anotherurls.
            await dbProvider.InsertAnotherLink(parsedUrl.Links);

            await dbProvider.InsertImages(parsedUrl.Images);
        }

        /// <summary>
        /// Метод обрабатывает исключения, возникшие при парсинге сайтов.
        /// </summary>
        /// <param name="url">Ссылка на страницу, при парсинге которой возникло исключение.</param>
        /// <param name="ex">Информация об исключении.</param>
        async Task HandleExceptionAsync(string url, Exception ex)
        {
            // Выводим в консоль информацию об исключении.
            lock (consoleLock)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Ошибочка с ссылкой: {url} " + ex.Message);
                Console.ForegroundColor = ConsoleColor.White;
            }
            // Удаляем текущую ссылку из таблицы anotherurls.
            await dbProvider.DeleteAnotherUrl(url);
            // Сохраняем информацию об ошибке в таблице unaccessedurl.
            await dbProvider.InsertUnaccessedUrl(url);
        }
    }
}
