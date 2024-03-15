using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Parse.Domain.Entities
{
    public class Robots
    {
        [Key]
        public string Host { get; set; }

        public string? Content { get; }

        //public List<string> AllowList
        //{
        //    get
        //    {

        //        return GetAllowList();


        //    }
        //    set
        //    {

        //    }
        //}
        [NotMapped]
        public List<string> DisallowList
        {
            get
            {
                return GetDisallowList();
            }
            set
            {

            }
        }

        //private List<string> GetAllowList()
        //{
        //    var list = new List<string>();
        //    var lines = Content.Split('\n');
        //    foreach (var line in lines)
        //    {
        //        if (line.StartsWith("Allow"))
        //        {
        //            list.Add(Host + line.Trim()[7..]);
        //        }
        //    }
        //    return list;
        //}

        private List<string> GetDisallowList()
        {
            var list = new List<string>();
            var lines = Content.Split('\n');
            foreach (var line in lines)
            {
                if (line.Trim().StartsWith("Disallow"))
                {
                    string disallow = line.Trim()[10..];
                    if (disallow.StartsWith('*') && disallow.EndsWith('*'))
                    {
                        list.Add(disallow.Trim('*'));
                    }
                    else
                    if (disallow[0] == '/' && disallow[1] == '*')
                    {
                        list.Add(disallow[2..]);
                    }

                }
            }
            return list;
        }

        public Robots(string host)
        {
            Host = host;
            var client = new HttpClient();
            string file = client.GetStringAsync($"https://{host}/robots.txt").Result;
            Content = GetCurrentAgentString(file);
        }

        public Robots(string host, string file)
        {
            Host = host;
            Content = file;
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
