using Npgsql;
using NpgsqlTypes;
using Parse.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Parse.Domain
{
    /// <summary>
    /// Класс PostgreDbProvider реализует интерфейс IDbProvider и предоставляет методы для взаимодействия с базой данных PostgreSQL.
    /// Методы класса позволяют выполнять операции чтения, вставки, обновления и удаления данных в таблицах базы данных.
    /// Класс использует библиотеку Npgsql для подключения к базе данных и выполнения SQL-запросов.
    /// </summary>
    public class PostgreDbProvider : IDbProvider
    {
        // Поле для хранения строки подключения к базе данных.
        // Инициализируется в конструкторе класса.
        private readonly string connectionString;


        // Конструктор класса.
        // Принимает строку подключения к базе данных и инициализирует поле connectionString.
        public PostgreDbProvider(string connectionString)
        {
            this.connectionString = connectionString;
        }

        /// <summary>
        /// Метод для получения количества записей в таблице anotherurls.
        /// </summary>
        /// <returns>Количество записей в таблице anotherurls в виде строки.</returns>
        public async Task<string> GetAnotherUrlsCount()
        {
            // Создание объекта NpgsqlConnection для подключения к базе данных.
            // Инициализируется с помощью строки подключения из поля connectionString.
            using NpgsqlConnection connection = new NpgsqlConnection(connectionString);
            // Открытие подключения к базе данных.
            connection.Open();

            // SQL-запрос для получения количества записей в таблице anotherurls.
            string sql = "SELECT count(*) FROM anotherurls";

            // Создание объекта NpgsqlCommand для выполнения SQL-запроса.
            // Инициализируется с помощью запроса sql и объекта подключения connection.
            using NpgsqlCommand cmd = new NpgsqlCommand(sql, connection);
            // Асинхронное выполнение запроса с помощью метода ExecuteReaderAsync().
            // Результат выполнения запроса сохраняется в объекте NpgsqlDataReader.
            using NpgsqlDataReader reader = await cmd.ExecuteReaderAsync();
            // Перемещение курсора на первую строку результатов запроса.
            reader.Read();
            // Закрытие подключения к базе данных.
            connection.Close();
            // Возврат количества записей в виде строки.
            return reader[0].ToString();

        }

        /// <summary>
        /// Метод для вставки данных из объекта ParsedUrl в таблицу urlandhtml.
        /// </summary>
        /// <param name="parsedUrl">Объект ParsedUrl, содержащий данные для вставки.</param>
        public async Task InsertParsedUrl(ParsedUrl parsedUrl)
        {
            // Создание объекта NpgsqlConnection для подключения к базе данных.
            using NpgsqlConnection con = new NpgsqlConnection(connectionString);
            // Открытие подключения к базе данных.
            con.Open();

            // SQL-запрос для вставки данных в таблицу urlandhtml.
            // Значения для вставки передаются в виде параметров с помощью синтаксиса @параметр.
            string sql = "INSERT INTO urlandhtml (url, title, text,links, date, vector, lang) VALUES (@url, @title, @text, @links, @date, @vector, @lang)";

            // Создание объекта NpgsqlCommand для выполнения SQL-запроса.
            using NpgsqlCommand cmd = new NpgsqlCommand(sql, con);
            // Добавление значений для параметров в объект cmd.Parameters.
            // Значения для параметров берутся из свойств объекта parsedUrl.
            cmd.Parameters.AddWithValue("url", parsedUrl.URL);
            cmd.Parameters.AddWithValue("title", parsedUrl.Title);
            cmd.Parameters.AddWithValue("text", parsedUrl.Text);
            cmd.Parameters.AddWithValue("links", parsedUrl.Links);
            cmd.Parameters.AddWithValue("date", parsedUrl.DateAdded);
            cmd.Parameters.AddWithValue("lang", parsedUrl.Lang);
            cmd.Parameters.AddWithValue("vector", parsedUrl.Vector);
            try
            {
                // Асинхронное выполнение запроса с помощью метода ExecuteNonQueryAsync().
                // Метод ExecuteNonQueryAsync() используется для выполнения запросов,
                // которые не возвращают результатов (например, INSERT, UPDATE, DELETE).
                await cmd.ExecuteNonQueryAsync();
                // Закрытие подключения к базе данных.
                con.Close();

            }
            catch
            {
                // Если произошла ошибка при выполнении запроса,
                // то счетчик i не увеличивается, и закрывается только текущее подключение.
                // Это позволяет избежать повторной вставки одних и тех же данных
                // при повторном вызове метода InsertParsedUrl().
            }
        }

        public async Task InsertImages(List<Image> images)
        {

            // Создание объекта NpgsqlConnection для подключения к базе данных.
            using NpgsqlConnection con = new NpgsqlConnection(connectionString);
            // Открытие подключения к базе данных.
            con.Open();

            // SQL-запрос для вставки строки в таблицу images.
            // Если строка уже существует в таблице, то вставка не выполняется.
            string sql = "INSERT INTO images (url, alt, title, sourceurl) VALUES (@url, @alt, @title, @sourceurl) ON CONFLICT DO NOTHING";

            // Создание объекта NpgsqlCommand для выполнения SQL-запроса.
            using NpgsqlCommand cmd = new NpgsqlCommand(sql, con);

            // Цикл по всем строкам в списке pages.
            foreach (var image in images)
            {
                // Добавление значения для параметров
                cmd.Parameters.AddWithValue("url", image.Url);
                cmd.Parameters.AddWithValue("alt", image.Alt is null ? DBNull.Value : image.Alt);
                cmd.Parameters.AddWithValue("title", image.Title is null ? DBNull.Value : image.Title);
                cmd.Parameters.AddWithValue("sourceurl", image.SourceUrl);
                // Асинхронное выполнение запроса с помощью метода ExecuteNonQueryAsync().
                await cmd.ExecuteNonQueryAsync();
                // Удаление всех параметров из объекта cmd.Parameters.
                // Это позволяет избежать повторного добавления одних и тех же параметров
                // при выполнении нескольких запросов в одном подключении.
                cmd.Parameters.Clear();
            }

            // Закрытие подключения к базе данных.
            con.Close();
        }

        /// <summary>
        /// Метод для вставки одной строки в таблицу urlqueue.
        /// </summary>
        /// <param name="url">Строка для вставки.</param>
        public async Task InsertUrlQueue(string url)
        {
            // Создание объекта NpgsqlConnection для подключения к базе данных.
            using NpgsqlConnection con = new NpgsqlConnection(connectionString);
            // Открытие подключения к базе данных.
            con.Open();

            // SQL-запрос для вставки строки в таблицу urlqueue.
            // Если строка уже существует в таблице, то вставка не выполняется.
            string sql = "INSERT INTO urlqueue (url) VALUES (@url) ON CONFLICT DO NOTHING";

            // Создание объекта NpgsqlCommand для выполнения SQL-запроса.
            using NpgsqlCommand cmd = new NpgsqlCommand(sql, con);
            // Добавление значения для параметра @url в объект cmd.Parameters.
            cmd.Parameters.AddWithValue("url", url);

            try
            {
                // Асинхронное выполнение запроса с помощью метода ExecuteNonQueryAsync().
                await cmd.ExecuteNonQueryAsync();
                // Закрытие подключения к базе данных.
                con.Close();

            }
            catch
            {
                // Если произошла ошибка при выполнении запроса,
                // то текущее подключение закрывается, и ошибка не передается выше.
            }
        }

        /// <summary>
        /// Метод для вставки списка строк в таблицу urlqueue.
        /// </summary>
        /// <param name="pages">Список строк для вставки.</param>
        public async Task InsertUrlQueue(List<string> pages)
        {
            // Создание объекта NpgsqlConnection для подключения к базе данных.
            using NpgsqlConnection con = new NpgsqlConnection(connectionString);
            // Открытие подключения к базе данных.
            con.Open();

            // SQL-запрос для вставки строки в таблицу urlqueue.
            // Если строка уже существует в таблице, то вставка не выполняется.
            string sql = "INSERT INTO urlqueue (url) VALUES (@url)  ON CONFLICT DO NOTHING";

            // Создание объекта NpgsqlCommand для выполнения SQL-запроса.
            using NpgsqlCommand cmd = new NpgsqlCommand(sql, con);

            // Цикл по всем строкам в списке pages.
            foreach (var page in pages)
            {
                // Добавление значения для параметра @url в объект cmd.Parameters.
                cmd.Parameters.AddWithValue("url", page);
                // Асинхронное выполнение запроса с помощью метода ExecuteNonQueryAsync().
                await cmd.ExecuteNonQueryAsync();
                // Удаление всех параметров из объекта cmd.Parameters.
                // Это позволяет избежать повторного добавления одних и тех же параметров
                // при выполнении нескольких запросов в одном подключении.
                cmd.Parameters.Clear();
            }

            // Закрытие подключения к базе данных.
            con.Close();
        }

        /// <summary>
        /// Метод для вставки списка строк в таблицу anotherurls.
        /// </summary>
        /// <param name="urls">Список строк для вставки.</param>
        public async Task InsertAnotherLink(List<string> urls)
        {
            // Создание объекта NpgsqlConnection для подключения к базе данных.
            using NpgsqlConnection con = new NpgsqlConnection(connectionString);
            // Открытие подключения к базе данных.
            con.Open();

            // Цикл по всем строкам в списке urls.
            foreach (string url in urls)
            {
                // SQL-запрос для вставки строки в таблицу anotherurls.
                // Если строка уже существует в таблице, то вставка не выполняется.
                string sql = "INSERT INTO anotherurls (url) VALUES (@url) ON CONFLICT DO NOTHING ";

                // Создание объекта NpgsqlCommand для выполнения SQL-запроса.
                using NpgsqlCommand cmd = new NpgsqlCommand(sql, con);
                // Добавление значения для параметра @url в объект cmd.Parameters.
                cmd.Parameters.AddWithValue("url", url);
                // Асинхронное выполнение запроса с помощью метода ExecuteNonQueryAsync().
                await cmd.ExecuteNonQueryAsync();
            }

            // Закрытие подключения к базе данных.
            con.Close();
        }

        /// <summary>
        /// Метод для удаления одной строки из таблицы anotherurls.
        /// </summary>
        /// <param name="url">Строка для удаления.</param>
        public async Task DeleteAnotherUrl(string url)
        {
            // Создание объекта NpgsqlConnection для подключения к базе данных.
            using NpgsqlConnection con = new NpgsqlConnection(connectionString);
            // Открытие подключения к базе данных.
            con.Open();

            // SQL-запрос для удаления строки из таблицы anotherurls по значению поля url.
            string sql = "DELETE FROM anotherurls WHERE url=@url";

            // Создание объекта NpgsqlCommand для выполнения SQL-запроса.
            using NpgsqlCommand cmd = new NpgsqlCommand(sql, con);
            // Добавление значения для параметра @url в объект cmd.Parameters.
            cmd.Parameters.AddWithValue("url", url);

            try
            {
                // Асинхронное выполнение запроса с помощью метода ExecuteNonQueryAsync().
                await cmd.ExecuteNonQueryAsync();
                // Закрытие подключения к базе данных.
                con.Close();
            }
            catch
            {
                // Если произошла ошибка при выполнении запроса,
                // то текущее подключение закрывается, и ошибка не передается выше.
            }
        }

        /// <summary>
        /// Метод для получения списка строк из таблицы anotherurls.
        /// </summary>
        /// <returns>Список строк из таблицы anotherurls.</returns>
        public async Task<List<string>> GetAnotherUrls()
        {
            // Создание объекта NpgsqlConnection для подключения к базе данных.
            using NpgsqlConnection connection = new NpgsqlConnection(connectionString);
            // Открытие подключения к базе данных.
            connection.Open();

            // SQL-запрос для получения списка строк из таблицы anotherurls.
            string sql = "SELECT url FROM anotherurls";

            // Создание объекта NpgsqlCommand для выполнения SQL-запроса.
            NpgsqlCommand cmd = new NpgsqlCommand(sql, connection);
            // Асинхронное выполнение запроса с помощью метода ExecuteReaderAsync().
            // Результат выполнения запроса сохраняется в объекте NpgsqlDataReader.
            using NpgsqlDataReader reader = await cmd.ExecuteReaderAsync();

            // Создание списка для хранения результатов запроса.
            List<string> AnotherLinks = new List<string>();

            // Цикл по всем строкам результатов запроса.
            while (reader.Read())
            {
                // Добавление текущей строки результатов в список AnotherLinks.
                AnotherLinks.Add(reader[0].ToString());
            }

            // Закрытие подключения к базе данных.
            connection.Close();

            // Возврат списка AnotherLinks.
            return AnotherLinks;

        }

        /// <summary>
        /// Метод для очистки таблицы anotherurls.
        /// </summary>
        public async Task ClearAnotherUrls()
        {
            // Создание объекта NpgsqlConnection для подключения к базе данных.
            using NpgsqlConnection con = new NpgsqlConnection(connectionString);
            // Открытие подключения к базе данных.
            con.Open();

            // SQL-запрос для удаления всех строк из таблицы anotherurls.
            string sql = "DELETE FROM anotherurls";

            // Создание объекта NpgsqlCommand для выполнения SQL-запроса.
            using NpgsqlCommand cmd = new NpgsqlCommand(sql, con);
            // Асинхронное выполнение запроса с помощью метода ExecuteNonQueryAsync().
            await cmd.ExecuteNonQueryAsync();

            // Закрытие подключения к базе данных.
            con.Close();
        }

        /// <summary>
        /// Метод для проверки существования строки в таблице urlandhtml.
        /// </summary>
        /// <param name="url">Строка для проверки.</param>
        /// <returns>True, если строка существует в таблице, иначе false.</returns>
        public async Task<bool> ContainsUrlandhtml(string url)
        {
            // Создание объекта NpgsqlConnection для подключения к базе данных.
            using NpgsqlConnection con = new NpgsqlConnection(connectionString);
            // Открытие подключения к базе данных.
            con.Open();

            // SQL-запрос для получения строки из таблицы urlandhtml по значению поля url.
            string sql = "SELECT url FROM urlandhtml WHERE url=@url";

            // Создание объекта NpgsqlCommand для выполнения SQL-запроса.
            using NpgsqlCommand cmd = new NpgsqlCommand(sql, con);
            // Добавление значения для параметра @url в объект cmd.Parameters.
            cmd.Parameters.AddWithValue("url", url);

            // Асинхронное выполнение запроса с помощью метода ExecuteReaderAsync().
            // Результат выполнения запроса сохраняется в объекте NpgsqlDataReader.
            using NpgsqlDataReader reader = await cmd.ExecuteReaderAsync();

            // Если результат запроса содержит хотя бы одну строку,
            // то значит, что строка существует в таблице, и метод возвращает true.
            // В противном случае метод возвращает false.
            if (reader.Read())
            {
                var res = reader[0].ToString() == url;
                con.Close();
                return res;
            }
            else
            {
                con.Close();
                return false;
            }
        }

        /// <summary>
        /// Метод для обновления значений поля cluster в таблице urlandhtml.
        /// </summary>
        /// <param name="clusters">Список значений для обновления.</param>
        public async Task UpdateClusters(List<int> clusters)
        {
            // Создание объекта NpgsqlConnection для подключения к базе данных.
            using NpgsqlConnection con = new NpgsqlConnection(connectionString);
            // Открытие подключения к базе данных.
            con.Open();

            // Цикл по всем значениям в списке clusters.
            for (int i = 0; i < clusters.Count; i++)
            {
                // SQL-запрос для обновления значения поля cluster в таблице urlandhtml
                // по значению поля id.
                string sql = $"UPDATE urlandhtml SET cluster = @cluster WHERE id = @id";

                // Создание объекта NpgsqlCommand для выполнения SQL-запроса.
                using NpgsqlCommand cmd = new NpgsqlCommand(sql, con);
                // Добавление значений для параметров @cluster и @id в объект cmd.Parameters.
                cmd.Parameters.AddWithValue("cluster", clusters[i]);
                cmd.Parameters.AddWithValue("id", i + 1);

                try
                {
                    // Асинхронное выполнение запроса с помощью метода ExecuteNonQueryAsync().
                    await cmd.ExecuteNonQueryAsync();
                }
                catch
                {
                    // Если произошла ошибка при выполнении запроса,
                    // то текущее подключение не закрывается, и ошибка не передается выше.
                    // Это позволяет избежать повторного обновления одних и тех же значений
                    // при повторном вызове метода UpdateClusters().
                }
            }

            // Закрытие подключения к базе данных.
            con.Close();
        }

        /// <summary>
        /// Метод для вставки данных из списка кортежей в таблицу centroids.
        /// </summary>
        /// <param name="result">Список кортежей, содержащих данные для вставки.</param>
        public async Task InsertCentroids(List<Tuple<double[], int>> result)
        {
            // Создание объекта NpgsqlConnection для подключения к базе данных.
            using NpgsqlConnection con = new NpgsqlConnection(connectionString);
            // Открытие подключения к базе данных.
            con.Open();

            // Цикл по всем кортежам в списке result.
            foreach (var item in result)
            {
                // SQL-запрос для вставки данных в таблицу centroids.
                string sql = "INSERT INTO centroids (clusternum, vector) VALUES (@clusternum, @vector)";

                // Создание объекта NpgsqlCommand для выполнения SQL-запроса.
                using NpgsqlCommand cmd = new NpgsqlCommand(sql, con);
                // Добавление значений для параметров @clusternum и @vector в объект cmd.Parameters.
                cmd.Parameters.AddWithValue("clusternum", item.Item2);
                cmd.Parameters.AddWithValue("vector", item.Item1);

                try
                {
                    // Асинхронное выполнение запроса с помощью метода ExecuteNonQueryAsync().
                    await cmd.ExecuteNonQueryAsync();
                }
                catch
                {
                    // Если произошла ошибка при выполнении запроса,
                    // то текущее подключение не закрывается, и ошибка не передается выше.
                    // Это позволяет избежать повторной вставки одних и тех же данных
                    // при повторном вызове метода InsertCentroids().
                }
            }

            // Закрытие подключения к базе данных.
            con.Close();
        }

        /// <summary>
        /// Метод для вставки строки в таблицу unaccessedurl.
        /// </summary>
        /// <param name="url">Строка для вставки.</param>
        public async Task InsertUnaccessedUrl(string url)
        {
            // Создание объекта NpgsqlConnection для подключения к базе данных.
            using NpgsqlConnection con = new NpgsqlConnection(connectionString);
            // Открытие подключения к базе данных.
            con.Open();

            // SQL-запрос для вставки строки в таблицу unaccessedurl.
            // Если строка уже существует в таблице, то вставка не выполняется.
            string sql = "INSERT INTO unaccessedurl (url) VALUES (@url) ON CONFLICT DO NOTHING ";

            // Создание объекта NpgsqlCommand для выполнения SQL-запроса.
            using NpgsqlCommand cmd = new NpgsqlCommand(sql, con);
            // Добавление значения для параметра @url в объект cmd.Parameters.
            cmd.Parameters.AddWithValue("url", url);

            // Асинхронное выполнение запроса с помощью метода ExecuteNonQueryAsync().
            await cmd.ExecuteNonQueryAsync();

            // Закрытие подключения к базе данных.
            con.Close();
        }

        /// <summary>
        /// Метод для проверки существования строки в таблице unaccessedurl.
        /// </summary>
        /// <param name="url">Строка для проверки.</param>
        /// <returns>True, если строка существует в таблице, иначе false.</returns>
        public async Task<bool> ContainsUnaccessedUrll(string url)
        {
            // Создание объекта NpgsqlConnection для подключения к базе данных.
            using NpgsqlConnection con = new NpgsqlConnection(connectionString);
            // Открытие подключения к базе данных.
            con.Open();

            // SQL-запрос для получения строки из таблицы unaccessedurl по значению поля url.
            string sql = "SELECT url FROM unaccessedurl WHERE url=@url";

            // Создание объекта NpgsqlCommand для выполнения SQL-запроса.
            using NpgsqlCommand cmd = new NpgsqlCommand(sql, con);
            // Добавление значения для параметра @url в объект cmd.Parameters.
            cmd.Parameters.AddWithValue("url", url);

            // Асинхронное выполнение запроса с помощью метода ExecuteReaderAsync().
            // Результат выполнения запроса сохраняется в объекте NpgsqlDataReader.
            using NpgsqlDataReader reader = await cmd.ExecuteReaderAsync();

            // Если результат запроса содержит хотя бы одну строку,
            // то значит, что строка существует в таблице, и метод возвращает true.
            // В противном случае метод возвращает false.
            if (reader.Read())
            {
                var res = reader[0].ToString() == url;
                con.Close();
                return res;
            }
            else
            {
                con.Close();
                return false;
            }
        }

        /// <summary>
        /// Метод для вставки данных из объекта Domen в таблицу domen.
        /// При успешной вставке метод возвращает список всех доменов из таблицы domen.
        /// </summary>
        /// <param name="domen">Объект Domen, содержащий данные для вставки.</param>
        /// <returns>Список всех доменов из таблицы domen.</returns>
        public async Task<List<Domen>> InsertDomen(Domen domen)
        {
            // Создание объекта NpgsqlConnection для подключения к базе данных.
            using NpgsqlConnection con = new NpgsqlConnection(connectionString);
            // Открытие подключения к базе данных.
            con.Open();

            // SQL-запрос для вставки данных в таблицу domen.
            string sql = "INSERT INTO domen (host, sitemap, isparsed) VALUES (@host, @sitemap, @isparsed)";

            // Создание объекта NpgsqlCommand для выполнения SQL-запроса.
            using NpgsqlCommand cmd = new NpgsqlCommand(sql, con);
            // Добавление значений для параметров @host, @sitemap и @isparsed в объект cmd.Parameters.
            cmd.Parameters.AddWithValue("host", domen.Host);
            cmd.Parameters.AddWithValue("sitemap", domen.Sitemap.ToArray());
            cmd.Parameters.AddWithValue("isparsed", domen.isParsed);

            try
            {
                // Асинхронное выполнение запроса с помощью метода ExecuteNonQueryAsync().
                await cmd.ExecuteNonQueryAsync();
                // Закрытие текущего подключения.
                con.Close();
                // Возврат списка всех доменов из таблицы domen с помощью метода GetRobots().
                return await GetRobots();
            }
            catch (Exception ex)
            {
                // Если произошла ошибка при выполнении запроса,
                // то текущее подключение закрывается, и ошибка не передается выше.
                // Вместо этого метод возвращает список всех доменов из таблицы domen с помощью метода GetRobots().
                con.Close();
                return await GetRobots();
            }
        }

        /// <summary>
        /// Метод для обновления данных в таблице domen.
        /// </summary>
        /// <param name="domen">Объект Domen, содержащий данные для обновления.</param>
        public async Task UpdateDomen(Domen domen)
        {
            // Создание объекта NpgsqlConnection для подключения к базе данных.
            using NpgsqlConnection con = new NpgsqlConnection(connectionString);
            // Открытие подключения к базе данных.
            con.Open();

            // SQL-запрос для обновления данных в таблице domen по значению поля host.
            string sql = "UPDATE domen SET sitemap=@sitemap, isparsed=@isparsed WHERE host=@host";

            // Создание объекта NpgsqlCommand для выполнения SQL-запроса.
            using NpgsqlCommand cmd = new NpgsqlCommand(sql, con);
            // Добавление значений для параметров @host, @sitemap и @isparsed в объект cmd.Parameters.
            cmd.Parameters.AddWithValue("host", domen.Host);
            cmd.Parameters.AddWithValue("sitemap", domen.Sitemap.ToArray());
            cmd.Parameters.AddWithValue("isparsed", domen.isParsed);

            try
            {
                // Асинхронное выполнение запроса с помощью метода ExecuteNonQueryAsync().
                await cmd.ExecuteNonQueryAsync();
                // Закрытие текущего подключения.
                con.Close();
            }
            catch (Exception ex)
            {
                // Если произошла ошибка при выполнении запроса,
                // то текущее подключение закрывается, и ошибка не передается выше.
                con.Close();
            }
        }

        /// <summary>
        /// Метод для получения списка всех доменов из таблицы domen.
        /// </summary>
        /// <returns>Список всех доменов из таблицы domen.</returns>
        public async Task<List<Domen>> GetRobots()
        {
            // Создание объекта NpgsqlConnection для подключения к базе данных.
            using NpgsqlConnection con = new NpgsqlConnection(connectionString);
            // Инициализация списка для хранения результатов запроса.
            var result = new List<Domen>();
            // Открытие подключения к базе данных.
            con.Open();

            // SQL-запрос для получения всех строк из таблицы domen.
            string sql = "SELECT * FROM domen";

            // Создание объекта NpgsqlCommand для выполнения SQL-запроса.
            using NpgsqlCommand cmd = new NpgsqlCommand(sql, con);
            // Асинхронное выполнение запроса с помощью метода ExecuteReaderAsync().
            // Результат выполнения запроса сохраняется в объекте NpgsqlDataReader.
            using NpgsqlDataReader reader = await cmd.ExecuteReaderAsync();

            // Цикл по всем строкам результатов запроса.
            while (reader.Read())
            {
                // Создание объекта Domen на основе текущей строки результатов.
                result.Add(new Domen(reader[0].ToString(),
                    reader[1].ToString(),
                    reader[2].ToString(),
                    reader[3] is System.DBNull ? new List<string>() : ((string[])reader[3]).ToList()));
            }

            // Закрытие текущего подключения.
            con.Close();
            // Возврат списка result.
            return result;
        }

        /// <summary>
        /// Метод для получения списка всех объектов ParsedUrl из таблицы urlandhtml,
        /// у которых поле cluster не равно NULL.
        /// </summary>
        /// <returns>Список всех объектов ParsedUrl из таблицы urlandhtml, у которых поле cluster не равно NULL.</returns>
        public async Task<List<ParsedUrl>> GetParsedUrlsTexts()
        {
            // Создание объекта NpgsqlConnection для подключения к базе данных.
            using NpgsqlConnection con = new NpgsqlConnection(connectionString);
            // Инициализация списка для хранения результатов запроса.
            var result = new List<ParsedUrl>();
            // Открытие подключения к базе данных.
            con.Open();

            // SQL-запрос для получения всех строк из таблицы urlandhtml, у которых поле cluster не равно NULL.
            string sql = "SELECT url, text, vector, title FROM urlandhtml where cluster is not null";

            // Создание объекта NpgsqlCommand для выполнения SQL-запроса.
            using NpgsqlCommand cmd = new NpgsqlCommand(sql, con);
            // Асинхронное выполнение запроса с помощью метода ExecuteReaderAsync().
            // Результат выполнения запроса сохраняется в объекте NpgsqlDataReader.
            using NpgsqlDataReader reader = await cmd.ExecuteReaderAsync();

            // Цикл по всем строкам результатов запроса.
            while (reader.Read())
            {
                // Создание объекта ParsedUrl на основе текущей строки результатов.
                ParsedUrl parsedUrl = new ParsedUrl();
                parsedUrl.URL = reader[0].ToString();
                parsedUrl.Text = reader[1].ToString();
                parsedUrl.Vector = reader.GetFieldValue<double[]>(2);
                parsedUrl.Title = reader[3].ToString();
                result.Add(parsedUrl);
            }

            // Закрытие текущего подключения.
            con.Close();
            // Возврат списка result.
            return result;
        }

        /// <summary>
        /// Метод для удаления всех данных из таблиц anotherurls, domen, unaccessedurl и urlandhtml.
        /// </summary>
        public async Task Truncate()
        {
            // Создание объекта NpgsqlConnection для подключения к базе данных.
            using NpgsqlConnection connection = new NpgsqlConnection(connectionString);
            // Открытие подключения к базе данных.
            connection.Open();

            // SQL-запрос для удаления всех данных из таблиц anotherurls, domen, unaccessedurl и urlandhtml.
            string sql = "TRUNCATE anotherurls, domen,unaccessedurl, urlandhtml";

            // Создание объекта NpgsqlCommand для выполнения SQL-запроса.
            NpgsqlCommand cmd = new NpgsqlCommand(sql, connection);
            // Асинхронное выполнение запроса с помощью метода ExecuteReaderAsync().
            // Результат выполнения запроса сохраняется в объекте NpgsqlDataReader,
            // но в данном случае он не используется.
            using NpgsqlDataReader reader = await cmd.ExecuteReaderAsync();

            // Закрытие текущего подключения.
            connection.Close();
        }

        /// <summary>
        /// Метод для получения списка всех текстов из таблицы urlandhtml.
        /// </summary>
        /// <returns>Список всех текстов из таблицы urlandhtml.</returns>
        public async Task<List<string>> GetTexts()
        {
            // Создание объекта NpgsqlConnection для подключения к базе данных.
            using NpgsqlConnection connection = new NpgsqlConnection(connectionString);
            // Инициализация списка для хранения результатов запроса.
            List<string> texts = new List<string>();
            // Открытие подключения к базе данных.
            connection.Open();

            // SQL-запрос для получения всех текстов из таблицы urlandhtml.
            string sql = "select text from urlandhtml";

            // Создание объекта NpgsqlCommand для выполнения SQL-запроса.
            NpgsqlCommand cmd = new NpgsqlCommand(sql, connection);
            // Асинхронное выполнение запроса с помощью метода ExecuteReaderAsync().
            // Результат выполнения запроса сохраняется в объекте NpgsqlDataReader.
            using NpgsqlDataReader reader = await cmd.ExecuteReaderAsync();

            // Цикл по всем строкам результатов запроса.
            while (reader.Read())
            {
                // Добавление текущего текста в список texts.
                texts.Add(reader[0].ToString());
            }

            // Закрытие текущего подключения.
            connection.Close();
            // Возврат списка texts.
            return texts;
        }

        /// <summary>
        /// Метод для получения списка всех описаний из таблицы urlandhtml.
        /// </summary>
        /// <returns>Список всех описаний из таблицы urlandhtml.</returns>
        public async Task<List<string>> GetDescriptions()
        {
            // Создание объекта NpgsqlConnection для подключения к базе данных.
            using NpgsqlConnection connection = new NpgsqlConnection(connectionString);
            // Инициализация списка для хранения результатов запроса.
            List<string> descriptions = new List<string>();
            // Открытие подключения к базе данных.
            connection.Open();

            // SQL-запрос для получения всех описаний из таблицы urlandhtml.
            string sql = "SELECT description from urlandhtml";

            // Создание объекта NpgsqlCommand для выполнения SQL-запроса.
            NpgsqlCommand cmd = new NpgsqlCommand(sql, connection);
            // Асинхронное выполнение запроса с помощью метода ExecuteReaderAsync().
            // Результат выполнения запроса сохраняется в объекте NpgsqlDataReader.
            using NpgsqlDataReader reader = await cmd.ExecuteReaderAsync();

            // Цикл по всем строкам результатов запроса.
            while (reader.Read())
            {
                // Добавление текущего описания в список descriptions.
                descriptions.Add(reader[0].ToString());
            }

            // Закрытие текущего подключения.
            connection.Close();
            // Возврат списка descriptions.
            return descriptions;
        }

        public async Task<List<RequestEntity>> GetRequestEntities(int clusterNum, string request)
        {
            var entites = new List<RequestEntity>();
            using NpgsqlConnection connection = new NpgsqlConnection(connectionString);
            connection.Open();
            string finalSql = $"SELECT urlandhtml.url, urlandhtml.title, urlandhtml.text, images.url FROM urlandhtml JOIN images ON images.sourceurl = urlandhtml.url WHERE cluster = {clusterNum}";
            NpgsqlCommand finalCmd = new NpgsqlCommand(finalSql, connection);
            NpgsqlDataReader finalReader = finalCmd.ExecuteReader();

            while (finalReader.Read())
            {
                RequestEntity newEntity = new RequestEntity();
                newEntity.Url = finalReader[0].ToString();
                string entityHtml = finalReader[1].ToString();
                string entityText = finalReader[2].ToString();
                string entityImage = finalReader[3].ToString();

                Uri uri = new Uri(newEntity.Url);
                newEntity.Domain = uri.Host;

                newEntity.Tittle = entityHtml;
                newEntity.ImageSrc = entityImage;
                int index = entityText.IndexOf(request);
                int startIndex = Math.Max(0, index - 120);
                int length = Math.Min(entityText.Length - startIndex, request.Length + 240);
                newEntity.MatchContent = entityText.Substring(startIndex, length).Replace(request, $"<strong>{request}</strong>"); ;


                entites.Add(newEntity);
            }


            connection.Close();

            return entites;
        }

        public async Task<List<Centroid>> GetCentroids()
        {
            var res = new List<Centroid>();
            using NpgsqlConnection con = new NpgsqlConnection(connectionString);
            var allPages = new List<ParsedUrl>();
            con.Open();

            string sql = "SELECT clusternum,vector FROM centroids";

            using NpgsqlCommand cmd = new NpgsqlCommand(sql, con);

            using NpgsqlDataReader reader = await cmd.ExecuteReaderAsync();

            while (reader.Read())
            {
                Centroid centroid = new Centroid();
                centroid.clusterNum = Convert.ToInt32(reader[0]);
                centroid.vector = centroid.vector = reader.GetFieldValue<double[]>(1);
                res.Add(centroid);

            }
            con.Close();
            return res;
        }

        public async Task InsertApiEntity(ParsedUrl parsedUrl)
        {          
            // Создание объекта NpgsqlConnection для подключения к базе данных.
            using NpgsqlConnection con = new NpgsqlConnection(connectionString);
            // Открытие подключения к базе данных.
            con.Open();

            // SQL-запрос для вставки данных в таблицу domen.
            string sql = "INSERT INTO urlandhtml (url, text, title, datepublished, vector) VALUES (@url, @text, @title, @datepublished, @vector) on conflict do nothing";

            // Создание объекта NpgsqlCommand для выполнения SQL-запроса.
            using NpgsqlCommand cmd = new NpgsqlCommand(sql, con);
            // Добавление значений для параметров @host, @sitemap и @isparsed в объект cmd.Parameters.
            cmd.Parameters.AddWithValue("url", parsedUrl.URL);
            cmd.Parameters.AddWithValue("text", parsedUrl.Text);
            cmd.Parameters.AddWithValue("title", parsedUrl.Title);
            cmd.Parameters.AddWithValue("datepublished", parsedUrl.DatePublished);
            cmd.Parameters.AddWithValue("vector", parsedUrl.Vector);

            try
            {
                // Асинхронное выполнение запроса с помощью метода ExecuteNonQueryAsync().
                var res = await cmd.ExecuteNonQueryAsync();

                // Закрытие текущего подключения.
                con.Close();
            }
            catch (Exception ex)
            {
                con.Close();
            }
        }
    }
}
