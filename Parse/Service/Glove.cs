using DeepMorphy;
using Newtonsoft.Json;
using System.Text.RegularExpressions;

namespace Parse.Service
{
    class Glove
    {
        Lemmatizator lemmatizator;

        public Dictionary<string, double[]> Model { get; }

        private int vectorScale;

        private int windowSize;

        private double learningRate;

        private double a = 0.75;

        public Glove(int vectorScale = 50, int windowSize = 2, double learningRate = 0.01)
        {
            this.vectorScale = vectorScale;
            this.windowSize = windowSize;
            this.learningRate = learningRate;
            Model = new Dictionary<string, double[]>();
            lemmatizator = new Lemmatizator();
        }

        public void Learn(string[] texts, int iterations)
        {
            var textsWords = GetTextsWords(texts);
            var dictionary = GetDictionary(textsWords);

            var matrix = GetCommonOccuranceMatrix(textsWords, dictionary);

            InitializeModel(dictionary);

            var xmax = GetMax(matrix);

            for (int i = 0; i < Model.Count; i++)
            {
                for (int j = i; j < Model.Count; j++)
                {
                    if (matrix[i, j] != 0)
                    {
                        Console.WriteLine($"i={i}     j={j}    {ComputeLoss(Model[dictionary[i]], Model[dictionary[j]], matrix[i, j])}");
                    }
                }
            }


            //Parallel.For(0, iterations, i =>
            //{
            //    UpdateEmbeddingsAndComputeLoss(matrix, dictionary, xmax);
            //});

            for (int iteration = 0; iteration < iterations; iteration++)
            {
                UpdateEmbeddingsAndComputeLoss(matrix, dictionary, xmax);
            }

            for (int i = 0; i < Model.Count; i++)
            {
                for (int j = i; j < Model.Count; j++)
                {
                    if (matrix[i, j] != 0)
                    {
                        Console.WriteLine($"i={i}     j={j}    {ComputeLoss(Model[dictionary[i]], Model[dictionary[j]], matrix[i, j])}");
                    }
                }
            }
        }

        private void InitializeModel(List<string> dictionary)
        {
            foreach (var word in dictionary)
            {
                var doubles = Enumerable.Range(0, vectorScale)
                                        .Select(_ => Random.Shared.Next(-9999, 10000) / 10000.0)
                                        .ToArray();
                if (word is null)
                {
                    int i = 0;
                }
                Model.Add(word, doubles);
            }
        }

        private void UpdateEmbeddingsAndComputeLoss(int[,] matrix, List<string> dictionary, int xmax)
        {
            //Parallel.For(0, dictionary.Count, i =>
            //{
            //    string word1 = dictionary[i];
            //    Parallel.For(0, dictionary.Count, j =>
            //    {
            //        if (i != j)
            //        {
            //            int coOccurrenceCount = matrix[i, j];
            //            if (coOccurrenceCount != 0)
            //            {

            //                string word2 = dictionary[j];

            //                var weight = F(coOccurrenceCount, xmax);
            //                var gradients = ComputeGradient(Model[word1], Model[word2], coOccurrenceCount, weight);

            //                lock (Model)
            //                {
            //                    UpdateEmbeddings(word1, gradients.Item1, learningRate);
            //                    UpdateEmbeddings(word2, gradients.Item2, learningRate);
            //                }
            //            }
            //        }
            //    });
            //});
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
                    var gradients = ComputeGradient(Model[word1], Model[word2], coOccurrenceCount, weight);

                    lock (Model)
                    {
                        UpdateEmbeddings(word1, gradients.Item1, learningRate);
                        UpdateEmbeddings(word2, gradients.Item2, learningRate);
                    }
                }
            }
        }

        public void UpdateEmbeddings(string word, double[] gradient, double learningRate)
        {
            //Parallel.For(0, Model[word].Length, i =>
            //{
            //    Model[word][i] -= learningRate * gradient[i];
            //});
            for (int i = 0; i < Model[word].Length; i++)
            {
                Model[word][i] -= learningRate * gradient[i];
            }
        }

        public (double[], double[]) ComputeGradient(double[] embedding1, double[] embedding2, double cooccurrenceCount, double weight)
        {
            double innerProduct = Dot(embedding1, embedding2);

            double logCooccurrence = Math.Log(cooccurrenceCount);

            double[] gradient1 = new double[embedding1.Length];
            double[] gradient2 = new double[embedding2.Length];

            for (int i = 0; i < embedding1.Length; i++)
            {
                gradient1[i] = weight * (innerProduct - logCooccurrence) * embedding2[i];
                gradient2[i] = weight * (innerProduct - logCooccurrence) * embedding1[i];
            }

            return (gradient1, gradient2);
        }

        public async Task<double[]> GetTextVector(string text)
        {
            int matchCount = 0;
            List<double[]> wordVectorsInText = new List<double[]>();
            foreach (string word in RegexMatches.RemovePunctuation(text).Split(' '))
            {
                if (!string.IsNullOrWhiteSpace(word) && Model.TryGetValue(lemmatizator.GetLemma(word), out double[] values))
                {
                    wordVectorsInText.Add(values);
                    matchCount++;
                }
            }
            double[] averageVector = new double[wordVectorsInText.First().Length];
            if (wordVectorsInText.Any())
            {
                for (int i = 0; i < wordVectorsInText.First().Length; i++)
                    averageVector[i] = wordVectorsInText.Select(w => w[i]).Average();
            }
            Console.WriteLine($"Всего слов в тексте: {text.Split(' ').Count()}");
            Console.WriteLine($"Найдено совпадений: {matchCount}");
            Console.WriteLine(" ");
            return averageVector;
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

        static int GetMax(int[,] matrix)
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

        private List<List<string>> GetTextsWords(string[] texts)
        {
            var res = texts.Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(text => RegexMatches.RemovePunctuation(text)
                                         .ToLower()
                                         .Split(" ")
                                         .Where(s => !string.IsNullOrWhiteSpace(s))
                                         .Select(s => lemmatizator.GetLemma(s))
                                         .ToList())
                        .ToList();
            lemmatizator.Save();
            return res;
        }

        private List<string> GetDictionary(List<List<string>> texts)
        {
            string jsonFilePath = "stopwords-ru.json";
            var stopwords = JsonConvert.DeserializeObject<List<string>>(File.ReadAllText(jsonFilePath));
            return texts.SelectMany(text => text.Where(t => !string.IsNullOrWhiteSpace(t)))
                        .Distinct()
                        //.Where(word => !stopwords.Contains(word))
                        .ToList();
        }

        int[,] GetCommonOccuranceMatrix(List<List<string>> textsWords, List<string> dictionary)
        {
            int[,] commonMatrix = new int[dictionary.Count, dictionary.Count];

            Parallel.ForEach(textsWords, text =>
            {
                for (int i = 0; i < text.Count; i++)
                {
                    for (int j = i + 1; j < i + windowSize + 1; j++)
                    {
                        if (j < text.Count)
                        {
                            int indexWord1 = dictionary.IndexOf(text[i]);
                            int indexWord2 = dictionary.IndexOf(text[j]);

                            if (indexWord1 != -1 && indexWord2 != -1 && indexWord1 != indexWord2)
                            {
                                lock (commonMatrix)
                                {
                                    commonMatrix[indexWord1, indexWord2]++;
                                    commonMatrix[indexWord2, indexWord1]++;
                                }
                            }
                        }
                    }
                }
            });

            return commonMatrix;
        }

        public void Save()
        {
            FileManager.SaveModel(Model);
        }
    }
}
