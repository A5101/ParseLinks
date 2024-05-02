using DeepMorphy;
using Newtonsoft.Json;
using System.Text.RegularExpressions;
using System.Threading;

namespace Parse.Service
{
    public class Glove
    {
        Lemmatizator lemmatizator;

        public Dictionary<string, double[]> Model { get; }

        private int vectorScale;

        private int windowSize;

        private double learningRate;

        private double a = 0.75;

        public Glove(int vectorScale = 50, int windowSize = 2, double learningRate = 0.01, bool useReadyModel = false)
        {
            this.vectorScale = vectorScale;
            this.windowSize = windowSize;
            this.learningRate = learningRate;
            Model = FileManager.OpenModel(useReadyModel: useReadyModel, fileName:@"model.json");
            lemmatizator = new Lemmatizator();
        }

        public async Task Learn(string[] texts, int iterations)
        {
            var textsWords = GetTextsWords(texts);
            var dictionary = GetDictionary(textsWords);
            lemmatizator.Save();
            Console.WriteLine($"В словаре {dictionary.Count}");

            var matrix = await GetCommonOccuranceMatrix(textsWords, dictionary);

            //var write = new StreamWriter(@"matrix.json");
           // write.WriteLine(JsonConvert.SerializeObject(matrix));
           // write.Close();

            InitializeModel(dictionary);

            var xmax = matrix.GetMax();
            Console.WriteLine($"Максимум {xmax}");

            string s = "1";
            while ((s = Console.ReadLine()) != "exit")
            {
                iterations = int.Parse(s);
                double d = 0;
                int count = 0;
                for (int i = 0; i < 1; i++)
                {
                    for (int j = i; j < 20; j++)
                    {
                        if (matrix[i, j] != 0)
                        {
                            //count++;
                          //  d += ComputeLoss(Model[dictionary[i]], Model[dictionary[j]], matrix[i, j]);
                            Console.WriteLine($"i={i} j={j} {ComputeLoss(Model[dictionary[i]], Model[dictionary[j]], matrix[i, j])}");
                        }
                    }
                }
                //Console.WriteLine(count);
                //Console.WriteLine(d);

                //Parallel.For(0, iterations, i =>
                //{
                //    UpdateEmbeddingsAndComputeLoss(matrix, dictionary, xmax);
                //});

                var t1 = DateTime.Now;
                for (int iteration = 0; iteration < iterations; iteration++)
                {
                    Console.WriteLine($"Итерация {iteration}");
                    await UpdateEmbeddingsAndComputeLoss(matrix, dictionary, xmax, 1000);
                }
                Console.WriteLine($"{DateTime.Now.Subtract(t1)}");


                d = 0;
                for (int i = 0; i < 1; i++)
                {
                    for (int j = i; j < 20; j++)
                    {
                        if (matrix[i, j] != 0)
                        {
                            //d += ComputeLoss(Model[dictionary[i]], Model[dictionary[j]], matrix[i, j]);
                            Console.WriteLine($"i={i} j={j} {ComputeLoss(Model[dictionary[i]], Model[dictionary[j]], matrix[i, j])}");
                        }
                    }
                }
              //  Console.WriteLine(d);
            }
        }

        private async Task UpdateEmbeddingsAndComputeLoss(SparseMatrix matrix, List<string> dictionary, int xmax, int partsCount = 10)
        {
            int partSize = partsCount == 1 ? dictionary.Count / partsCount : dictionary.Count / (partsCount - 1);
            for (int i = 0; i < partsCount; i++)
            {
                var tasks = new List<Task>();
                for (int threadId = 0; threadId < partsCount; threadId++)
                {
                    int i_StartIndex = threadId * partSize;
                    int i_EndIndex = (threadId + 1) * partSize;

                    int j_StartIndex = (i + threadId) * partSize;
                    int j_EndIndex = (i + threadId + 1) * partSize;

                    if (j_StartIndex > dictionary.Count)
                    {
                        int corr = dictionary.Count - partsCount * partSize;
                        j_StartIndex -= dictionary.Count - corr;
                        j_EndIndex -= dictionary.Count - corr;
                    }

                    // Console.WriteLine($"Поток {threadId} обрабатывает квадрат i: {i_StartIndex} - {i_EndIndex};   j: {j_StartIndex} - {j_EndIndex}");
                    tasks.Add(Task.Factory.StartNew(() => UpdateEmbeddingsAndComputeLoss(matrix, i_StartIndex, i_EndIndex, j_StartIndex, j_EndIndex, dictionary, xmax)));
                }
                //Console.WriteLine();
                await Task.WhenAll(tasks);
            }
        }

        private void UpdateEmbeddingsAndComputeLoss(SparseMatrix matrix, int i_StartIndex, int i_EndIndex, int j_StartIndex, int j_EndIndex, List<string> dictionary, int xmax)
        {
            for (int i = i_StartIndex; i < i_EndIndex; i++)
            {
                if (i < dictionary.Count)
                {
                    var word1 = dictionary[i];
                    var wordEmbedding1 = Model[word1];

                    for (int j = j_StartIndex; j < j_EndIndex; j++)
                    {
                        if (j < dictionary.Count)
                        {
                            if (i == j) continue;
                            int coOccurrenceCount = matrix[i, j];
                            if (coOccurrenceCount == 0) continue;
                            string word2 = dictionary[j];

                            var weight = F(coOccurrenceCount, xmax);
                            var gradients = ComputeGradient(wordEmbedding1, Model[word2], coOccurrenceCount, weight);

                            UpdateEmbeddings(word1, gradients.Item1, learningRate);
                            UpdateEmbeddings(word2, gradients.Item2, learningRate);
                        }
                    }
                }
            }
        }

        public void UpdateEmbeddings(string word, double[] gradient, double learningRate)
        {
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
            //Parallel.ForEach(RegexMatches.RemovePunctuation(text).Split(' '), word =>
            //{
            //    if (!string.IsNullOrWhiteSpace(word) && Model.TryGetValue(lemmatizator.GetLemma(word), out double[] values))
            //    {
            //        wordVectorsInText.Add(values);
            //        matchCount++;
            //    }
            //});
            foreach (string word in RegexMatches.RemovePunctuation(text).ToLower().Split(' '))
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


        private void InitializeModel(List<string> dictionary)
        {
            Model.Clear();
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

        double F(double x, double xmax)
        {
            return Math.Pow(x / xmax, a);
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


        async Task<SparseMatrix> GetCommonOccuranceMatrix(List<List<string>> textsWords, List<string> dictionary)
        {
            SparseMatrix commonMatrix = new SparseMatrix();
            Dictionary<string, int> dict = new Dictionary<string, int>();
            for (int i = 0; i < dictionary.Count; i++)
            {
                dict.Add(dictionary[i], i);
            }

            int count = 0;
            foreach(var text in textsWords)
            {
                
                Parallel.For(0, text.Count, i =>
                {
                    Parallel.For(i + 1, i + windowSize + 1, j =>
                    {
                        if (j < text.Count)
                        {
                            if (!string.IsNullOrWhiteSpace(text[i]) && !string.IsNullOrWhiteSpace(text[j]))
                            {
                                int indexWord1 = dict[text[i]];
                                int indexWord2 = dict[text[j]];

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
                    });
                });
                Console.WriteLine($"Текст {++count} обработан");
            };

            return commonMatrix;
        }

        public void Save()
        {
            FileManager.SaveModel(Model);
        }
    }
}
