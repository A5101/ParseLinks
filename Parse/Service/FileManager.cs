using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Word2vec.Tools;

namespace Parse.Service
{
    public static class FileManager
    {
        public static void SaveLemms(Dictionary<string, string> lemms)
        {
            var write = new StreamWriter(@"lemms.json");
            write.WriteLine(JsonConvert.SerializeObject(lemms));
            write.Close();
        }

        public static Dictionary<string, string> OpenLemms()
        {
            if (File.Exists(@"lemms.json"))
            {
                var read = new StreamReader(@"lemms.json");
                var res = JsonConvert.DeserializeObject<Dictionary<string, string>>(read.ReadToEnd());
                read.Close();
                return res;
            }
            return new Dictionary<string, string>();
        }

        public static void SaveModel(Dictionary<string, double[]> model)
        {
            var write = new StreamWriter(@"model.json");
            write.WriteLine(JsonConvert.SerializeObject(model));
            write.Close();
        }

        public static Dictionary<string, double[]> OpenModel(string modelPath)
        {
            if (File.Exists(modelPath))
            {
                using (StreamReader file = File.OpenText(modelPath))
                {
                    JsonSerializer serializer = new JsonSerializer();
                    var dictionary = (Dictionary<string, double[]>)serializer.Deserialize(file, typeof(Dictionary<string, double[]>));
                    return dictionary;
                }
            }
            else
            {
                return new Dictionary<string, double[]>();
            }
        }
    }
}
