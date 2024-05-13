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

            string sql = "INSERT INTO urlandhtml (url, title, text,links, date, meta) VALUES (@url, @title, @text, @links, @date, @meta)";

            using NpgsqlCommand cmd = new NpgsqlCommand(sql, con);
            cmd.Parameters.AddWithValue("url", parsedUrl.URL);
            cmd.Parameters.AddWithValue("title", parsedUrl.Title);
            cmd.Parameters.AddWithValue("text", parsedUrl.Text);
            cmd.Parameters.AddWithValue("links", parsedUrl.Links);
            cmd.Parameters.AddWithValue("date", parsedUrl.DateAdded);
            cmd.Parameters.AddWithValue("meta", parsedUrl.Meta);
            // cmd.Parameters.AddWithValue("description", parsedUrl.Description);

            try
            {
                await cmd.ExecuteNonQueryAsync();
                con.Close();

            }
            catch
            {
            }
        }

        public async Task InsertUrlQueue(string url)
        {
            using NpgsqlConnection con = new NpgsqlConnection(connectionString);
            con.Open();

            string sql = "INSERT INTO urlqueue (url) VALUES (@url) ON CONFLICT DO NOTHING";

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


        public async Task InsertUrlQueue(List<string> pages)
        {
            using NpgsqlConnection con = new NpgsqlConnection(connectionString);
            con.Open();

            string sql = "INSERT INTO urlqueue (url) VALUES (@url)  ON CONFLICT DO NOTHING";

            using NpgsqlCommand cmd = new NpgsqlCommand(sql, con);

            foreach (var page in pages)
            {
                cmd.Parameters.AddWithValue("url", page);
                await cmd.ExecuteNonQueryAsync();
                cmd.Parameters.Clear();
            }

            con.Close();
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

        public async Task<List<Domen>> InsertDomen(Domen domen)
        {
            using NpgsqlConnection con = new NpgsqlConnection(connectionString);
            con.Open();

            //string sql = "INSERT INTO domen (host, file, rss) VALUES (@host, @file, @rss)";

            string sql = "INSERT INTO domen (host, sitemap, isparsed) VALUES (@host, @sitemap, @isparsed)";

            using NpgsqlCommand cmd = new NpgsqlCommand(sql, con);
            cmd.Parameters.AddWithValue("host", domen.Host);
            cmd.Parameters.AddWithValue("sitemap", domen.Sitemap.ToArray());
            cmd.Parameters.AddWithValue("isparsed", domen.isParsed);
            //cmd.Parameters.AddWithValue("file", domen.Content);
            //cmd.Parameters.AddWithValue("rss", domen.RssLink);

            try
            {
                await cmd.ExecuteNonQueryAsync();
                con.Close();
                return await GetRobots();
            }
            catch(Exception ex)
            {
                con.Close();
                return await GetRobots();
            }
        }

        public async Task UpdateDomen(Domen domen)
        {
            using NpgsqlConnection con = new NpgsqlConnection(connectionString);
            con.Open();

            string sql = "UPDATE domen SET sitemap=@sitemap, isparsed=@isparsed WHERE host=@host";

            using NpgsqlCommand cmd = new NpgsqlCommand(sql, con);
            cmd.Parameters.AddWithValue("host", domen.Host);
            cmd.Parameters.AddWithValue("sitemap", domen.Sitemap.ToArray());
            cmd.Parameters.AddWithValue("isparsed", domen.isParsed);
            //cmd.Parameters.AddWithValue("file", domen.Content);
            //cmd.Parameters.AddWithValue("rss", domen.RssLink);

            try
            {
                await cmd.ExecuteNonQueryAsync();
                con.Close();
            }
            catch (Exception ex)
            {
                con.Close();
            }
        }



        public async Task<List<Domen>> GetRobots()
        {
            using NpgsqlConnection con = new NpgsqlConnection(connectionString);
            var result = new List<Domen>();
            con.Open();

            string sql = "SELECT * FROM domen";

            using NpgsqlCommand cmd = new NpgsqlCommand(sql, con);

            using NpgsqlDataReader reader = await cmd.ExecuteReaderAsync();

            while (reader.Read())
            {
                result.Add(new Domen(reader[0].ToString(), 
                    reader[1].ToString(),
                    reader[2].ToString(),
                    reader[3] is System.DBNull ? new List<string>() : ((string[])reader[3]).ToList()));
            }
            con.Close();
            return result;
        }
        public async Task<List<ParsedUrl>> GetParsedUrlsTexts()
        {
            using NpgsqlConnection con = new NpgsqlConnection(connectionString);
            var result = new List<ParsedUrl>();
            con.Open();

            string sql = "SELECT url,text FROM urlandhtml";

            using NpgsqlCommand cmd = new NpgsqlCommand(sql, con);

            using NpgsqlDataReader reader = await cmd.ExecuteReaderAsync();

            while (reader.Read())
            {
                ParsedUrl parsedUrl = new ParsedUrl();
                parsedUrl.URL = reader[0].ToString();
                parsedUrl.Text = reader[1].ToString();
                result.Add(parsedUrl);
            }
            con.Close();
            return result;
        }

        public async Task Truncate()
        {
            using NpgsqlConnection connection = new NpgsqlConnection(connectionString);
            connection.Open();

            string sql = "TRUNCATE anotherurls, domen,unaccessedurl, urlandhtml";
            NpgsqlCommand cmd = new NpgsqlCommand(sql, connection);
            using NpgsqlDataReader reader = await cmd.ExecuteReaderAsync();
            connection.Close();
        }

        public async Task<List<string>> GetTexts()
        {
            using NpgsqlConnection connection = new NpgsqlConnection(connectionString);
            connection.Open();

            string sql = "select text from urlandhtml";
            NpgsqlCommand cmd = new NpgsqlCommand(sql, connection);
            using NpgsqlDataReader reader = await cmd.ExecuteReaderAsync();
            StringBuilder text = new StringBuilder("");
            List<string> texts = new List<string>();
            while (reader.Read())
            {
                texts.Add(reader[0].ToString());
            }
            connection.Close();
            return texts;
        }
        public async Task<List<string>> GetDescriptions()
        {
            using NpgsqlConnection connection = new NpgsqlConnection(connectionString);
            connection.Open();

            string sql = "SELECT description from urlandhtml";
            NpgsqlCommand cmd = new NpgsqlCommand( sql, connection);
            using NpgsqlDataReader reader = await cmd.ExecuteReaderAsync();
            List<string> descriptions = new List<string>();
            while (reader.Read())
            {
                descriptions.Add(reader[0].ToString());
            }
            connection.Close();

            return descriptions;
        }
    }
}
