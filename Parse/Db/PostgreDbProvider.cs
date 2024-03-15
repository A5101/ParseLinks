using Npgsql;
using Parse.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Parse.Domain
{
    internal class PostgreDbProvider : IDbProvider
    {
        private readonly string connectionString;

        public PostgreDbProvider(string connectionString)
        {
            this.connectionString = connectionString;
        }

        public async Task<string> GetAnotherUrlsCount()
        {
            using NpgsqlConnection connection = new NpgsqlConnection(connectionString);
            connection.Open();

            string sql = "SELECT count(*) FROM anotherurls";

            using NpgsqlCommand cmd = new NpgsqlCommand(sql, connection);
            using NpgsqlDataReader reader = await cmd.ExecuteReaderAsync();
            reader.Read();
            connection.Close();
            return reader[0].ToString();

        }

        public async Task InsertParsedUrl(ParsedUrl parsedUrl)
        {
            using NpgsqlConnection con = new NpgsqlConnection(connectionString);
            con.Open();

            string sql = "INSERT INTO urlandhtml (url, title, text,links, date) VALUES (@url, @title, @text, @links, @date)";

            using NpgsqlCommand cmd = new NpgsqlCommand(sql, con);
            cmd.Parameters.AddWithValue("url", parsedUrl.URL);
            cmd.Parameters.AddWithValue("title", parsedUrl.Title);
            cmd.Parameters.AddWithValue("text", parsedUrl.Text);
            cmd.Parameters.AddWithValue("links", parsedUrl.Links);
            cmd.Parameters.AddWithValue("date", parsedUrl.DateAdded);

            try
            {
                await cmd.ExecuteNonQueryAsync();
                con.Close();
                // Console.WriteLine("Вставил новую запаршенную ссылку " + urlEntity.URL + " в " + DateTime.Now);

            }
            catch
            {
                // Console.WriteLine("Небольшая заминочка на ссылке: " + urlEntity.URL);
            }
        }

        public async Task InsertAnotherLink(List<string> urls)
        {
            using NpgsqlConnection con = new NpgsqlConnection(connectionString);
            con.Open();

            foreach (string url in urls)
            {
                string sql = "INSERT INTO anotherurls (url) VALUES (@url) ON CONFLICT DO NOTHING ";
                using NpgsqlCommand cmd = new NpgsqlCommand(sql, con);
                cmd.Parameters.AddWithValue("url", url);
                await cmd.ExecuteNonQueryAsync();              
            }
            con.Close();
        }

        public async Task DeleteAnotherUrl(string url)
        {
            using NpgsqlConnection con = new NpgsqlConnection(connectionString);
            con.Open();

            string sql = "DELETE FROM anotherurls WHERE url=@url";

            using NpgsqlCommand cmd = new NpgsqlCommand(sql, con);
            cmd.Parameters.AddWithValue("url", url);

            try
            {
                await cmd.ExecuteNonQueryAsync();
                con.Close();
            }
            catch
            {

            }
        }

        public async Task<List<string>> GetAnotherUrls()
        {
            using NpgsqlConnection connection = new NpgsqlConnection(connectionString);
            connection.Open();

            string sql = "SELECT url FROM anotherurls";
            NpgsqlCommand cmd = new NpgsqlCommand(sql, connection);
            using NpgsqlDataReader reader = await cmd.ExecuteReaderAsync();
            List<string> AnotherLinks = new List<string>();

            while (reader.Read())
            {
                AnotherLinks.Add(reader[0].ToString());
            }
            connection.Close();
            // await ClearAnotherUrls();
            return AnotherLinks;

        }

        public async Task ClearAnotherUrls()
        {
            using NpgsqlConnection con = new NpgsqlConnection(connectionString);
            con.Open();
            string sql = "DELETE FROM anotherurls";
            using NpgsqlCommand cmd = new NpgsqlCommand(sql, con);
            await cmd.ExecuteNonQueryAsync();
            con.Close();
        }

        public async Task<bool> ContainsUrlandhtml(string url)
        {
            using NpgsqlConnection con = new NpgsqlConnection(connectionString);
            con.Open();

            string sql = "SELECT url FROM urlandhtml WHERE url=@url";

            using NpgsqlCommand cmd = new NpgsqlCommand(sql, con);
            cmd.Parameters.AddWithValue("url", url);

            using NpgsqlDataReader reader = await cmd.ExecuteReaderAsync();

            if (reader.Read())
            {
                con.Close();
                return reader[0].ToString() == url;
            }
            else
            {
                con.Close();
                return false;
            }
        }

        public async Task InsertUnaccessedUrl(string url)
        {
            using NpgsqlConnection con = new NpgsqlConnection(connectionString);
            con.Open();
            string sql = "INSERT INTO unaccessedurl (url) VALUES (@url) ON CONFLICT DO NOTHING ";
            using NpgsqlCommand cmd = new NpgsqlCommand(sql, con);
            cmd.Parameters.AddWithValue("url", url);
            await cmd.ExecuteNonQueryAsync();
            con.Close();
        }

        public async Task<bool> ContainsUnaccessedUrll(string url)
        {
            using NpgsqlConnection con = new NpgsqlConnection(connectionString);
            con.Open();

            string sql = "SELECT url FROM unaccessedurl WHERE url=@url";

            using NpgsqlCommand cmd = new NpgsqlCommand(sql, con);
            cmd.Parameters.AddWithValue("url", url);

            using NpgsqlDataReader reader = await cmd.ExecuteReaderAsync();

            if (reader.Read())
            {
                con.Close();
                return reader[0].ToString() == url;
            }
            else
            {
                con.Close(); 
                return false;
            }
        }

        public async Task<List<Robots>> InsertRobots(Robots robots)
        {
            using NpgsqlConnection con = new NpgsqlConnection(connectionString);
            con.Open();

            string sql = "INSERT INTO robots (host, file) VALUES (@host, @file)";

            using NpgsqlCommand cmd = new NpgsqlCommand(sql, con);
            cmd.Parameters.AddWithValue("host", robots.Host);
            cmd.Parameters.AddWithValue("file", robots.Content);

            try
            {
                await cmd.ExecuteNonQueryAsync();
                con.Close();
                return await GetRobots();
            }
            catch
            {
                con.Close();
                return await GetRobots();
            }
        }

        public async Task<List<Robots>> GetRobots()
        {
            using NpgsqlConnection con = new NpgsqlConnection(connectionString);
            var result = new List<Robots>();
            con.Open();

            string sql = "SELECT * FROM robots";

            using NpgsqlCommand cmd = new NpgsqlCommand(sql, con);

            using NpgsqlDataReader reader = await cmd.ExecuteReaderAsync();

            while (reader.Read())
            {
                result.Add(new Robots(reader[0].ToString(), reader[1].ToString()));
            }
            con.Close();
            return result;
        }

        public async Task Truncate()
        {
            using NpgsqlConnection connection = new NpgsqlConnection(connectionString);
            connection.Open();

            string sql = "TRUNCATE anotherurls, robots,unaccessedurl, urlandhtml";
            NpgsqlCommand cmd = new NpgsqlCommand(sql, connection);
            using NpgsqlDataReader reader = await cmd.ExecuteReaderAsync();
            connection.Close();
        }
    }
}
