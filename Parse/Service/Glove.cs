using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Parse.Service
{
    class Glove
    {
        public Dictionary<string, double[]> model { get; }

        private int vectorScale;

        private int windowSize;

        private double learningRate;

        private double a = 0.75;

        public Glove(int vectorScale = 50, int windowSize = 2, double learningRate = 0.01)
        {
            this.vectorScale = vectorScale;
            this.windowSize = windowSize;
            this.learningRate = learningRate;
            model = new Dictionary<string, double[]>();
        }

        public void Learn(string[] texts, int iterations)
        {
            var textsWords = GetTextsWords(texts);

            var dictionary = GetDictionary(textsWords);



            var matrix = GetCommonOccuranceMatrix(textsWords, dictionary);

            foreach (var word in dictionary)
            {
                double[] doubles = Enumerable.Range(0, vectorScale)
                                             .Select(_ => Random.Shared.Next(-9999, 10000) / 10000.0)
                                             .ToArray();
                model.Add(word, doubles);
            }

            int xmax = GetMax(matrix);

            int i1 = 0, j1 = 3;

            double lossij = ComputeLoss(model[dictionary[i1]], model[dictionary[j1]], matrix[i1, j1]);


            for (int iteration = 0; iteration < iterations; iteration++)
            {
                for (int i = 0; i < dictionary.Count; i++)
                {
                    string word1 = dictionary[i];
                    for (int j = 0; j < dictionary.Count; j++)
                    {
                        if (i == j) continue;
                        int coOccurrenceCount = matrix[i, j];
                        if (coOccurrenceCount == 0) continue;

                        string word2 = dictionary[j];

                        var weight = F(coOccurrenceCount, xmax);
                        var gradients = ComputeGradient(model[word1], model[word2], coOccurrenceCount, weight);

                        UpdateEmbeddings(word1, gradients.Item1, learningRate);
                        UpdateEmbeddings(word2, gradients.Item2, learningRate);
                    }
                }
            }

            double lossijNew = ComputeLoss(model[dictionary[i1]], model[dictionary[j1]], matrix[i1, j1]);
        }

        public void UpdateEmbeddings(string word, double[] gradient, double learningRate)
        {
            for (int i = 0; i < model[word].Length; i++)
            {
                model[word][i] -= learningRate * gradient[i];
            }
        }

        public (double[], double[]) ComputeGradient(double[] embedding1, double[] embedding2, double cooccurrenceCount, double weight)
        {
            double innerProduct = Dot(embedding1, embedding2);

            double logCooccurrence = Math.Log(cooccurrenceCount);
            double loss = weight * Math.Pow(innerProduct - logCooccurrence, 2) / 2;

            double[] gradient1 = new double[embedding1.Length];
            double[] gradient2 = new double[embedding2.Length];

            for (int i = 0; i < embedding1.Length; i++)
            {
                gradient1[i] = weight * (innerProduct - logCooccurrence) * embedding2[i];
                gradient2[i] = weight * (innerProduct - logCooccurrence) * embedding1[i];
            }

            return (gradient1, gradient2);
        }

        double ComputeLoss(double[] vector1, double[] vector2, int coOccurrenceCount)
        {
            double dotProduct = Dot(vector1, vector2);
            double bias1 = 0;
            double bias2 = 0;

            double loss = 0.5 * Math.Pow(dotProduct + bias1 + bias2 - Math.Log(coOccurrenceCount), 2);
            return loss;
        }

        double F(double x, double xmax)
        {
            return Math.Pow(x / xmax, a);
        }

        int GetMax(int[,] matrix)
        {
            int xmax = 0;
            for (int i = 0; i < matrix.GetLength(0); i++)
            {
                for (int j = 0; j < matrix.GetLength(1); j++)
                {
                    if (xmax < matrix[i, j])
                    {
                        xmax = matrix[i, j];
                    }
                }
            }

            return xmax;
        }

        double Dot(double[] v1, double[] v2)
        {
            return v1.Select((v, i) => v * v2[i]).Sum();
        }

        List<List<string>> GetTextsWords(string[] texts)
        {
            var res = new List<List<string>>();

            foreach (var text in texts)
            {
                var newText = RemovePunctuation(text).ToLower().Split(" ").ToList();
                newText.RemoveAll(s => s == "");
                newText.ForEach(s =>
                {
                    s = s.Replace("\n", "");
                    s = s.Replace("\r", "");
                });
                res.Add(newText);
            }

            return res;
        }

        static List<string> GetDictionary(List<List<string>> texts)
        {
            List<string> dictionary = new List<string>();
            string jsonFilePath = "stopwords-ru.json";

            string jsonText = File.ReadAllText(jsonFilePath);

            var stopwords = JsonConvert.DeserializeObject<List<string>>(jsonText);

            foreach (var text in texts)
            {
                foreach (var word in text)
                {
                    if (!dictionary.Contains(word) && !stopwords.Contains(word))
                    {
                        dictionary.Add(word);
                    }
                }
            }


            return dictionary;
        }

        int[,] GetCommonOccuranceMatrix(List<List<string>> textsWords, List<string> dictionary)
        {
            int[,] commonMatrix = new int[dictionary.Count, dictionary.Count];

            Parallel.ForEach(textsWords, text =>
            {
                foreach (var word in text)
                {
                    int startIndex = text.IndexOf(word);
                    Parallel.For(startIndex, text.Count, i =>
                    {
                        for (int j = i + 1; j < i + windowSize + 1; j++)
                        {
                            if (j < text.Count)
                            {
                                int indexWord1 = dictionary.IndexOf(text[i]);
                                int indexWord2 = dictionary.IndexOf(text[j]);

                                if (indexWord1 != -1 && indexWord2 != -1)
                                {
                                    // Синхронизируем доступ к общей матрице
                                    lock (commonMatrix)
                                    {
                                        commonMatrix[indexWord1, indexWord2]++;
                                        commonMatrix[indexWord2, indexWord1]++;
                                    }
                                }
                            }
                        }
                    });
                }
            });

            return commonMatrix;
        }


        //int[,] GetCommonOccuranceMatrix(List<List<string>> textsWords, List<string> dictionary)
        //{
        //    int[,] commonMatrix = new int[dictionary.Count, dictionary.Count];

        //    foreach (var text in textsWords)
        //    {
        //        for (int i = 0; i < text.Count; i++)
        //        {
        //            for (int j = i + 1; j < i + windowSize + 1; j++)
        //            {
        //                if (j < text.Count)
        //                {
        //                    int indexWord1 = dictionary.IndexOf(text[i]);
        //                    int indexWord2 = dictionary.IndexOf(text[j]);

        //                    if (indexWord1 != -1 && indexWord2 != -1)
        //                    {
        //                        commonMatrix[indexWord1, indexWord2]++;
        //                        commonMatrix[indexWord2, indexWord1]++;
        //                    }
        //                }
        //            }
        //        }
        //    }

        //    return commonMatrix;
        //}

        int[,] SumMatrix(int[,] matrix1, int[,] matrix2)
        {
            int[,] res = new int[matrix1.GetLength(0), matrix1.GetLength(1)];

            for (int i = 0; i < matrix1.GetLength(0); i++)
            {
                for (int j = 0; j < matrix1.GetLength(1); j++)
                {
                    res[i, j] = matrix1[i, j] + matrix2[i, j];
                }
            }

            return res;
        }

        static string RemovePunctuation(string input)
        {
            return Regex.Replace(input, @"[\p{P}\p{S}]", " ");
        }

        public void Save()
        {
            var write = new StreamWriter(@"model.json");
            write.WriteLine(JsonConvert.SerializeObject(model));
            write.Close();
        }
    }
}
