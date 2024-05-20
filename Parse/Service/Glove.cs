using DeepMorphy;
using Newtonsoft.Json;
using System.Text.RegularExpressions;
using System.Threading;

namespace Parse.Service
{
    /// <summary>
    /// Класс Glove реализует алгоритм обучения векторных представлений слов с использованием метода GloVe (Global Vectors for Word Representation).
    /// </summary>
    public class Glove
    {
        /// <summary>
        /// Лемматизатор для приведения слов к их начальной форме.
        /// </summary>
        Lemmatizator lemmatizator;

        /// <summary>
        /// Словарь, представляющий модель векторных представлений слов.
        /// </summary>
        public Dictionary<string, double[]> Model { get; }

        /// <summary>
        /// Размерность вектора.
        /// </summary>
        private int vectorScale;

        /// <summary>
        /// Размер окна контекста.
        /// </summary>
        private int windowSize;

        /// <summary>
        /// Скорость обучения.
        /// </summary>
        private double learningRate;

        /// <summary>
        /// Параметр для функции взвешивания.
        /// </summary>
        private double a = 0.75;

        /// <summary>
        /// Конструктор класса Glove.
        /// </summary>
        /// <param name="vectorScale">Размерность вектора. По умолчанию 50.</param>
        /// <param name="windowSize">Размер окна контекста. По умолчанию 2.</param>
        /// <param name="learningRate">Скорость обучения. По умолчанию 0.01.</param>
        /// <param name="modelPath">Путь к файлу модели.</param>
        public Glove(int vectorScale = 50, int windowSize = 2, double learningRate = 0.01, string modelPath = "")
        {
            this.vectorScale = vectorScale;
            this.windowSize = windowSize;
            this.learningRate = learningRate;
            Model = FileManager.OpenModel(modelPath: modelPath);
            lemmatizator = new Lemmatizator();
        }

        /// <summary>
        /// Запускает процесс обучения модели на основе заданных текстов.
        /// </summary>
        /// <param name="texts">Массив текстов для обучения.</param>
        /// <param name="iterations">Количество итераций обучения.</param>
        /// <param name="skipInitialize">Флаг, указывающий, нужно ли пропустить инициализацию модели.</param>
        public async Task Learn(string[] texts, int iterations, bool skipInitialize = false)
        {
            // Получаем слова из текстов
            var textsWords = GetTextsWords(texts);
            // Создаем словарь слов
            var dictionary = GetDictionary(textsWords);
            lemmatizator.Save();
            Console.WriteLine($"В словаре {dictionary.Count}");

            // Получаем матрицу совместных встречаемостей слов
            var matrix = await GetCommonOccuranceMatrix(textsWords, dictionary);

            // Инициализируем модель, если не указано пропустить инициализацию
            if (!skipInitialize)
            {
                InitializeModel(dictionary);
            }

            // Определяем максимальное значение в матрице
            var xmax = matrix.GetMax();
            Console.WriteLine($"Максимум {xmax}");

            // Начинаем итерации обучения
            string s = "1";
            Console.Write("Количество итераций, для продолжения exit: ");
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
                            Console.WriteLine($"i={i} j={j} {ComputeLoss(Model[dictionary[i]], Model[dictionary[j]], matrix[i, j])}");
                        }
                    }
                }

                var t1 = DateTime.Now;
                for (int iteration = 0; iteration < iterations; iteration++)
                {
                    Console.WriteLine($"Итерация {iteration}");
                    await UpdateEmbeddingsAndComputeLoss(matrix, dictionary, xmax, 100);
                }
                Console.WriteLine($"{DateTime.Now.Subtract(t1)}");

                d = 0;
                for (int i = 0; i < 1; i++)
                {
                    for (int j = i; j < 20; j++)
                    {
                        if (matrix[i, j] != 0)
                        {
                            Console.WriteLine($"i={i} j={j} {ComputeLoss(Model[dictionary[i]], Model[dictionary[j]], matrix[i, j])}");
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Обновляет векторные представления слов и вычисляет функцию потерь.
        /// </summary>
        /// <param name="matrix">Матрица совместных встречаемостей слов.</param>
        /// <param name="dictionary">Словарь слов.</param>
        /// <param name="xmax">Максимальное значение встречаемости слов.</param>
        /// <param name="partsCount">Количество частей для разделения данных при обучении.</param>
        private async Task UpdateEmbeddingsAndComputeLoss(SparseMatrix matrix, List<string> dictionary, int xmax, int partsCount = 10)
        {
            // Определяем размер части
            int partSize = partsCount == 1 ? dictionary.Count / partsCount : dictionary.Count / (partsCount - 1);
            for (int i = 0; i < partsCount; i++)
            {
                // Формируем список задач для параллельной обработки матрицы
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

                    // Запускаем задачу обновления эмбеддингов и вычисления потерь
                    tasks.Add(Task.Factory.StartNew(() => UpdateEmbeddingsAndComputeLoss(matrix, i_StartIndex, i_EndIndex, j_StartIndex, j_EndIndex, dictionary, xmax)));
                }
                await Task.WhenAll(tasks);
            }
        }

        /// <summary>
        /// Обновляет векторные представления слов и вычисляет функцию потерь для заданных индексов.
        /// </summary>
        /// <param name="matrix">Матрица совместных встречаемостей слов.</param>
        /// <param name="i_StartIndex">Начальный индекс для первого слова.</param>
        /// <param name="i_EndIndex">Конечный индекс для первого слова.</param>
        /// <param name="j_StartIndex">Начальный индекс для второго слова.</param>
        /// <param name="j_EndIndex">Конечный индекс для второго слова.</param>
        /// <param name="dictionary">Словарь слов.</param>
        /// <param name="xmax">Максимальное значение встречаемости слов.</param>
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

        /// <summary>
        /// Обновляет векторные представления слов на основе градиентов.
        /// </summary>
        /// <param name="word">Слово для обновления.</param>
        /// <param name="gradient">Градиент для обновления.</param>
        /// <param name="learningRate">Скорость обучения.</param>
        public void UpdateEmbeddings(string word, double[] gradient, double learningRate)
        {
            for (int i = 0; i < Model[word].Length; i++)
            {
                Model[word][i] -= learningRate * gradient[i];
            }
        }

        /// <summary>
        /// Вычисляет градиент для обновления векторных представлений слов.
        /// </summary>
        /// <param name="embedding1">Векторное представление первого слова.</param>
        /// <param name="embedding2">Векторное представление второго слова.</param>
        /// <param name="cooccurrenceCount">Количество совместных встречаемостей слов.</param>
        /// <param name="weight">Взвешенное значение встречаемости.</param>
        /// <returns>Кортеж с градиентами для обоих слов.</returns>
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

        /// <summary>
        /// Вычисляет средний вектор для текста.
        /// </summary>
        /// <param name="text">Текст для вычисления.</param>
        /// <returns>Средний вектор текста.</returns>
        public async Task<double[]> GetTextVector(string text)
        {
            int matchCount = 0;
            List<double[]> wordVectorsInText = new List<double[]>();
            foreach (string word in RegexMatches.RemovePunctuation(text).ToLower().Split(' '))
            {
                if (!string.IsNullOrWhiteSpace(word) && Model.TryGetValue(word.ToLower(), out double[] values))
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

        /// <summary>
        /// Вычисляет функцию потерь для двух векторных представлений слов.
        /// </summary>
        /// <param name="vector1">Первый вектор.</param>
        /// <param name="vector2">Второй вектор.</param>
        /// <param name="coOccurrenceCount">Количество совместных встречаемостей слов.</param>
        /// <returns>Значение функции потерь.</returns>
        double ComputeLoss(double[] vector1, double[] vector2, int coOccurrenceCount)
        {
            double dotProduct = Dot(vector1, vector2);
            double bias1 = 0;
            double bias2 = 0;
            double loss = 0.5 * Math.Pow(dotProduct + bias1 + bias2 - Math.Log(coOccurrenceCount), 2);
            return loss;
        }

        /// <summary>
        /// Инициализирует модель, создавая векторные представления для слов из словаря.
        /// </summary>
        /// <param name="dictionary">Словарь слов.</param>
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

        /// <summary>
        /// Функция взвешивания для вычисления градиента.
        /// </summary>
        /// <param name="x">Количество совместных встречаемостей слов.</param>
        /// <param name="xmax">Максимальное значение встречаемости слов.</param>
        /// <returns>Взвешенное значение.</returns>
        double F(double x, double xmax)
        {
            return Math.Pow(x / xmax, a);
        }

        /// <summary>
        /// Вычисляет скалярное произведение двух векторов.
        /// </summary>
        /// <param name="v1">Первый вектор.</param>
        /// <param name="v2">Второй вектор.</param>
        /// <returns>Скалярное произведение.</returns>
        double Dot(double[] v1, double[] v2)
        {
            return v1.Select((v, i) => v * v2[i]).Sum();
        }

        /// <summary>
        /// Извлекает слова из текстов и приводит их к начальной форме.
        /// </summary>
        /// <param name="texts">Массив текстов.</param>
        /// <returns>Список списков слов.</returns>
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

        /// <summary>
        /// Создает словарь слов из текстов.
        /// </summary>
        /// <param name="texts">Список списков слов.</param>
        /// <returns>Словарь слов.</returns>
        private List<string> GetDictionary(List<List<string>> texts)
        {
            string jsonFilePath = "stopwords-ru.json";
            var stopwords = JsonConvert.DeserializeObject<List<string>>(File.ReadAllText(jsonFilePath));
            return texts.SelectMany(text => text.Where(t => !string.IsNullOrWhiteSpace(t)))
                        .Distinct()
                        //.Where(word => !stopwords.Contains(word))
                        .ToList();
        }

        /// <summary>
        /// Создает матрицу совместных встречаемостей слов.
        /// </summary>
        /// <param name="textsWords">Список списков слов из текстов.</param>
        /// <param name="dictionary">Словарь слов.</param>
        /// <returns>Матрица совместных встречаемостей слов.</returns>
        async Task<SparseMatrix> GetCommonOccuranceMatrix(List<List<string>> textsWords, List<string> dictionary)
        {
            SparseMatrix commonMatrix = new SparseMatrix();
            Dictionary<string, int> dict = new Dictionary<string, int>();
            for (int i = 0; i < dictionary.Count; i++)
            {
                dict.Add(dictionary[i], i);
            }

            int count = 0;
            foreach (var text in textsWords)
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

        /// <summary>
        /// Сохраняет текущую модель в файл.
        /// </summary>
        public void Save()
        {
            FileManager.SaveModel(Model);
        }
    }
}
