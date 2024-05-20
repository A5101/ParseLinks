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
    /// <summary>
    /// Класс FileManager предоставляет методы для сохранения и загрузки данных модели и лемм.
    /// </summary>
    public static class FileManager
    {
        /// <summary>
        /// Сохраняет словарь лемм в файл "lemms.json".
        /// </summary>
        /// <param name="lemms">Словарь, содержащий леммы.</param>
        public static void SaveLemms(Dictionary<string, string> lemms)
        {
            // Создаем объект StreamWriter для записи в файл "lemms.json".
            var write = new StreamWriter(@"lemms.json");

            // Преобразуем словарь лемм в строку формата JSON и записываем ее в файл.
            write.WriteLine(JsonConvert.SerializeObject(lemms));

            // Закрываем поток записи, чтобы освободить ресурсы.
            write.Close();
        }

        /// <summary>
        /// Загружает словарь лемм из файла "lemms.json".
        /// </summary>
        /// <returns>Словарь, содержащий леммы.</returns>
        public static Dictionary<string, string> OpenLemms()
        {
            // Проверяем, существует ли файл "lemms.json".
            if (File.Exists(@"lemms.json"))
            {
                // Создаем объект StreamReader для чтения из файла "lemms.json".
                var read = new StreamReader(@"lemms.json");

                // Читаем все содержимое файла и десериализуем его в словарь.
                var res = JsonConvert.DeserializeObject<Dictionary<string, string>>(read.ReadToEnd());

                // Закрываем поток чтения, чтобы освободить ресурсы.
                read.Close();

                // Возвращаем десериализованный словарь.
                return res;
            }

            // Если файл не существует, возвращаем пустой словарь.
            return new Dictionary<string, string>();
        }

        /// <summary>
        /// Сохраняет модель в файл "model.json".
        /// </summary>
        /// <param name="model">Словарь, содержащий модель (векторные представления слов).</param>
        public static void SaveModel(Dictionary<string, double[]> model)
        {
            // Создаем объект StreamWriter для записи в файл "model.json".
            var write = new StreamWriter(@"model.json");

            // Преобразуем модель в строку формата JSON и записываем ее в файл.
            write.WriteLine(JsonConvert.SerializeObject(model));

            // Закрываем поток записи, чтобы освободить ресурсы.
            write.Close();
        }

        /// <summary>
        /// Загружает модель из указанного файла.
        /// </summary>
        /// <param name="modelPath">Путь к файлу модели.</param>
        /// <returns>Словарь, содержащий модель (векторные представления слов).</returns>
        public static Dictionary<string, double[]> OpenModel(string modelPath)
        {
            // Проверяем, существует ли файл по указанному пути.
            if (File.Exists(modelPath))
            {
                // Открываем файл для чтения.
                using (StreamReader file = File.OpenText(modelPath))
                {
                    // Создаем объект JsonSerializer для десериализации содержимого файла.
                    JsonSerializer serializer = new JsonSerializer();

                    // Десериализуем содержимое файла в словарь и возвращаем его.
                    var dictionary = (Dictionary<string, double[]>)serializer.Deserialize(file, typeof(Dictionary<string, double[]>));
                    return dictionary;
                }
            }
            else
            {
                // Если файл не существует, возвращаем пустой словарь.
                return new Dictionary<string, double[]>();
            }
        }
    }
}
