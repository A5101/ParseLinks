using Npgsql.EntityFrameworkCore.PostgreSQL.Query.Expressions.Internal;
using Parse.Domain;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace Parse.Service
{
    /// <summary>
    /// Класс KMeans реализует алгоритм кластеризации K-средних для работы с векторными представлениями слов.
    /// </summary>
    public class KMeans
    {
        /// <summary>
        /// Считывает модель векторных представлений слов из файла.
        /// </summary>
        /// <param name="filePath">Путь к файлу модели.</param>
        /// <returns>Словарь, содержащий векторные представления слов.</returns>
        public static Dictionary<string, double[]> ReadWordVectorsFromFile(string filePath)
        {
            // Создаем пустой словарь для хранения векторных представлений слов.
            Dictionary<string, double[]> wordVectors = new Dictionary<string, double[]>();

            // Открываем файл для чтения.
            using (StreamReader reader = new StreamReader(filePath))
            {
                string line;
                // Считываем файл построчно до конца.
                while ((line = reader.ReadLine()) != null)
                {
                    // Разделяем строку на части по пробелу.
                    string[] parts = line.Split(' ');
                    // Первое слово является ключом в словаре.
                    string word = parts[0];
                    // Создаем массив для хранения численных значений вектора.
                    double[] values = new double[parts.Length - 1];
                    // Преобразуем строки в числа и заполняем массив.
                    for (int i = 1; i < parts.Length; i++)
                    {
                        values[i - 1] = double.Parse(parts[i], CultureInfo.InvariantCulture);
                    }
                    // Добавляем слово и его векторное представление в словарь.
                    wordVectors.Add(word, values);
                }
            }
            // Возвращаем заполненный словарь.
            return wordVectors;
        }

        /// <summary>
        /// Вычисляет евклидово расстояние между двумя векторами.
        /// </summary>
        /// <param name="a">Первый вектор.</param>
        /// <param name="b">Второй вектор.</param>
        /// <returns>Евклидово расстояние между векторами.</returns>
        public static double EuclideanDistance(double[] a, double[] b)
        {
            double sum = 0;
            // Проходим по всем элементам векторов.
            for (int i = 0; i < a.Length; i++)
            {
                // Суммируем квадраты разностей соответствующих элементов.
                sum += Math.Pow(a[i] - b[i], 2);
            }
            // Возвращаем квадратный корень из суммы.
            return Math.Sqrt(sum);
        }

        /// <summary>
        /// Вычисляет центроид для кластера.
        /// </summary>
        /// <param name="data">Данные, представляющие векторы.</param>
        /// <param name="cluster">Список индексов, входящих в кластер.</param>
        /// <returns>Центроид кластера.</returns>
        private double[] CalculateCentroid(List<double[]> data, List<int> cluster)
        {
            // Создаем массив для хранения значений центроида.
            double[] centroid = new double[data[0].Length];
            // Проходим по всем точкам кластера.
            foreach (int index in cluster)
            {
                double[] point = data[index];
                // Суммируем значения каждого измерения.
                for (int i = 0; i < point.Length; i++)
                {
                    centroid[i] += point[i];
                }
            }
            // Делим сумму на количество точек, чтобы получить среднее значение.
            for (int i = 0; i < centroid.Length; i++)
            {
                centroid[i] /= cluster.Count;
            }
            // Возвращаем вычисленный центроид.
            return centroid;
        }

        /// <summary>
        /// Выполняет кластеризацию данных методом K-средних.
        /// </summary>
        /// <param name="data">Данные для кластеризации.</param>
        /// <param name="k">Количество кластеров.</param>
        /// <returns>Список индексов кластеров для каждой точки данных.</returns>
        public List<int> Cluster(List<double[]> data, int k)
        {
            // Строка подключения к базе данных.
            string connectionString = "Server=localhost; port=5432; user id=postgres; password=sa; database=WebSearchDB;";
            // Создаем объект Random для генерации случайных чисел.
            Random random = new Random();
            // Создаем массив для хранения центроидов.
            double[][] centroids = new double[k][];

            // Выбираем первую центроиду случайным образом.
            centroids[0] = data[random.Next(0, data.Count)];

            // Выбираем остальные центроиды.
            for (int centroidIndex = 1; centroidIndex < k; centroidIndex++)
            {
                Console.WriteLine($"Выбираем центроиду:{centroidIndex}");
                double[] distances = new double[data.Count];
                double totalDistance = 0;

                // Параллельно вычисляем расстояния до ближайшей центроиды для каждой точки данных.
                Parallel.For(0, data.Count, x =>
                {
                    double minDistance = double.MaxValue;
                    foreach (double[] centroid in centroids.Take(centroidIndex))
                    {
                        double distance = EuclideanDistance(data[x], centroid);
                        double distanceSquared = distance * distance;
                        minDistance = Math.Min(minDistance, distanceSquared);
                    }
                    distances[x] = minDistance;
                    totalDistance += minDistance;
                });

                // Выбираем новую центроиду на основе расстояний.
                double randValue = random.NextDouble() * totalDistance;
                double sum = 0;
                int dataIndex = 0;
                while (sum < randValue)
                {
                    sum += distances[dataIndex];
                    dataIndex++;
                }
                centroids[centroidIndex] = data[dataIndex - 1];
            }
            Console.WriteLine("Начальные центроиды выбраны..");
            List<int> clusters = new List<int>();

            bool centroidsChanged = true;
            int recalculationCount = 0;
            while (centroidsChanged)
            {
                recalculationCount++;
                centroidsChanged = false;
                List<int>[] newClusters = new List<int>[k];
                for (int i = 0; i < k; i++)
                {
                    newClusters[i] = new List<int>();
                }

                // Назначаем каждую точку данных ближайшему кластеру.
                for (int i = 0; i < data.Count; i++)
                {
                    double[] point = data[i];
                    double minDistance = double.MaxValue;
                    int minIndex = 0;
                    for (int j = 0; j < k; j++)
                    {
                        double distance = EuclideanDistance(point, centroids[j]);
                        if (distance < minDistance)
                        {
                            minDistance = distance;
                            minIndex = j;
                        }
                    }
                    newClusters[minIndex].Add(i);
                }

                // Пересчитываем центроиды.
                for (int i = 0; i < k; i++)
                {
                    double[] newCentroid = CalculateCentroid(data, newClusters[i]);
                    if (!centroids[i].SequenceEqual(newCentroid))
                    {
                        centroids[i] = newCentroid;
                        centroidsChanged = true;
                    }
                }
                Console.WriteLine($"Перерасчет центроидов завершен. Номер перерасчета: {recalculationCount}");
                clusters.Clear();
                for (int i = 0; i < data.Count; i++)
                {
                    for (int j = 0; j < k; j++)
                    {
                        if (newClusters[j].Contains(i))
                        {
                            clusters.Add(j);
                            break;
                        }
                    }
                }
            }
            List<Tuple<double[], int>> result = new List<Tuple<double[], int>>();
            for (int i = 0; i < k; i++)
            {
                result.Add(Tuple.Create(centroids[i], i));
            }
            // Вставка центроидов и кластеров в базу данных (код закомментирован).
            // var db = new PostgreDbProvider(connectionString);
            // db.InsertCentroids(result);
            // db.UpdateClusters(clusters);

            Console.WriteLine("Вставки в БД завершены");
            return clusters;
        }
    }
}
