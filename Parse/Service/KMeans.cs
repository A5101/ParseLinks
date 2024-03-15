using Parse.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Parse.Service
{
    internal class KMeans
    {
        /// <summary>
        /// Считываем модель
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        public static Dictionary<string, double[]> ReadWordVectorsFromFile(string filePath)
        {
            Dictionary<string, double[]> wordVectors = new Dictionary<string, double[]>();

            using (StreamReader reader = new StreamReader(filePath))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    string[] parts = line.Split(' ');
                    string word = parts[0];
                    double[] values = new double[parts.Length - 1];
                    for (int i = 1; i < parts.Length; i++)
                    {
                        values[i - 1] = double.Parse(parts[i], CultureInfo.InvariantCulture);
                    }
                    wordVectors.Add(word, values);
                }
            }
            return wordVectors;
        }
        /// <summary>
        /// Рассчет расстояния
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        private double EuclideanDistance(double[] a, double[] b)
        {
            double sum = 0;
            for (int i = 0; i < a.Length; i++)
            {
                sum += Math.Pow(a[i] - b[i], 2);
            }
            return Math.Sqrt(sum);
        }

        /// <summary>
        /// Вычисление центроид
        /// </summary>
        /// <param name="data"></param>
        /// <param name="cluster"></param>
        /// <returns></returns>
        private double[] CalculateCentroid(List<double[]> data, List<int> cluster)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Рассчет центроид");
            Console.ForegroundColor = ConsoleColor.Magenta;
            double[] centroid = new double[data[0].Length];
            foreach (int index in cluster)
            {
                double[] point = data[index];
                for (int i = 0; i < point.Length; i++)
                {
                    centroid[i] += point[i];
                }
            }
            for (int i = 0; i < centroid.Length; i++)
            {
                centroid[i] /= cluster.Count;
            }
            return centroid;
        }

        public List<int> Cluster(List<double[]> data, int k)
        {
            Random random = new Random();
            double[][] centroids = new double[k][];
            for (int i = 0; i < k; i++)
            {
                centroids[i] = data[random.Next(0, data.Count)];
            }

            List<int> clusters = new List<int>();

            bool centroidsChanged = true;
            while (centroidsChanged)
            {
                centroidsChanged = false;
                List<int>[] newClusters = new List<int>[k];
                for (int i = 0; i < k; i++)
                {
                    newClusters[i] = new List<int>();
                }

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

                for (int i = 0; i < k; i++)
                {
                    double[] newCentroid = CalculateCentroid(data, newClusters[i]);
                    if (!centroids[i].SequenceEqual(newCentroid))
                    {
                        centroids[i] = newCentroid;
                        centroidsChanged = true;
                    }
                }
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

            return clusters;
        }
    }
}