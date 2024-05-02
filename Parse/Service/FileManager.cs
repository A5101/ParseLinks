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

        public static Dictionary<string, double[]> OpenModel(bool useReadyModel = true, string fileName = @"data.json")
        {
            if (useReadyModel)
            {
                if (File.Exists(fileName))
                {
                    var read = new StreamReader(fileName);
                    //StringBuilder stringBuilder = new StringBuilder();
                    //string line;
                    //while ((line = read.ReadLine()) != null)
                    //{
                    //    stringBuilder.Append(line);
                    //}
                    using (var jsonReader = new JsonTextReader(read))
                    {
                        // Пропуск начального объекта (если необходимо)
                        if (!jsonReader.Read() || jsonReader.TokenType != JsonToken.StartObject)
                        {
                            throw new Exception("Expected start of object");
                        }

                        var dictionary = new Dictionary<string, double[]>();

                        // Чтение и десериализация JSON-данных
                        while (jsonReader.Read())
                        {
                            if (jsonReader.TokenType == JsonToken.PropertyName)
                            {
                                string key = jsonReader.Value.ToString();

                                if (!jsonReader.Read() || jsonReader.TokenType != JsonToken.StartArray)
                                {
                                    throw new Exception($"Expected start of array for key {key}");
                                }

                                var values = new List<double>();

                                // Чтение значений массива
                                while (jsonReader.Read() && jsonReader.TokenType != JsonToken.EndArray)
                                {
                                    if (jsonReader.TokenType == JsonToken.Float || jsonReader.TokenType == JsonToken.Integer)
                                    {
                                        values.Add(Convert.ToDouble(jsonReader.Value));
                                    }
                                }

                                dictionary[key] = values.ToArray();
                            }
                        }
                        read.Close();
                        return dictionary;
                    }
                    //var res = JsonConvert.DeserializeObject<Dictionary<string, double[]>>("");
                    read.Close();
                    //return res;
                }
            }
            return new Dictionary<string, double[]>();
        }
    }
}
