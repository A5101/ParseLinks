using Parse.Service;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Parse.Domain.Entities
{
    public class Domen
    {
        [Key]
        public string Host { get; set; }

        public string Content { get; set; }

        public List<string> Sitemap { get; set; }

        public string? RssLink { get; set; }

        public bool isParsed { get; set; } = false;

        //[NotMapped]
        //public List<string> DisallowList
        //{
        //    get
        //    {
        //        return GetDisallowList();
        //    }
        //    set
        //    {

        //    }
        //}

        //private List<string> GetDisallowList()
        //{
        //    var list = new List<string>();
        //    var lines = Content.Split('\n');
        //    foreach (var line in lines)
        //    {
        //        if (line.Trim().StartsWith("Disallow"))
        //        {
        //            string disallow = line.Trim()[10..];
        //            if (disallow.StartsWith('*') && disallow.EndsWith('*'))
        //            {
        //                list.Add(disallow.Trim('*'));
        //            }
        //            else
        //            if (disallow[0] == '/' && disallow[1] == '*')
        //            {
        //                list.Add(disallow[2..]);
        //            }

        //        }
        //    }
        //    return list;
        //}

        public Domen(string host)
        {
            try
            {
                Host = host;
                
            }
            catch
            {
                Host = host;
            }
        }

        public Domen(string host, string file, string rssLink, List<string> sitemap)
        {
            Host = host;
            Content = file;
            RssLink = rssLink;
            Sitemap = sitemap;
        }

        public async Task SetSiteMap()
        {
            var client = HttpClientFactory.Instance;
            var response = await client.GetAsync(Host + "/robots.txt");
            if (response.IsSuccessStatusCode)
            {
                try
                {
                    var resp = await response.Content.ReadAsStringAsync();
                    var file = resp.Split("\n")
                        .Select(str => str.ToLower())
                    .Where(str => str.StartsWith("sitemap"))
                    .Select(str => str.Replace("sitemap: ", "").Replace("\r", "").Replace("\n", ""))
                    .Select(str =>
                    {
                        if (str.StartsWith('/'))
                        {
                            str = Host + str;
                        }
                        return str;
                    })
                    .ToList();

                    Sitemap = file;
                }
                catch
                {

                }
            }
        }

        private string GetCurrentAgentString(string str)
        {
            string res;
            string findString = "User-Agent: *";
            int startIndex = str.IndexOf(findString);
            if (startIndex == -1)
            {
                startIndex = str.IndexOf("User-agent: *");
            }
            int endIndex = str.IndexOf("User-", startIndex + 1);
            if (endIndex != -1)
            {
                res = str.Substring(startIndex + findString.Length, endIndex - startIndex - findString.Length);
            }
            else
            {
                res = str.Substring(startIndex + findString.Length, str.Length - startIndex - findString.Length);
            }
            return res;
        }
    }
}
