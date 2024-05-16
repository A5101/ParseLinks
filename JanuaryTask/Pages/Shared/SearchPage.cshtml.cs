using JanuaryTask.Pages.Shared.Models;
using Parse.Domain;
using Parse.Service;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Npgsql;
using Parse.Domain.Entities;
using static System.Net.Mime.MediaTypeNames;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace JanuaryTask.Pages.Shared
{
    public class Centroid
    {
        public int clusterNum { get; set; }
        public double[] vector { get; set; }
    }

    public class CentroidWithDistance : Centroid
    {
        public int clusterNum { get; set; }
        public double[] vector { get; set; }
        public double distance { get; set; }
    }

    public class SearchPageModel : PageModel
    {
        public void OnGet()
        {

        }

        public async Task<IActionResult> OnPost(string request)
        {
            await GetFromDb(request);
            ViewData["SearchQuery"] = request;
            return Page();
        }

        static double CosDistance(double[] vector1, double[] vector2)
        {
            double magnitude1 = Math.Sqrt(vector1.Sum(x => x * x));
            double magnitude2 = Math.Sqrt(vector2.Sum(x => x * x));

            double dotProduct = 0;
            for (int i = 0; i < vector1.Length; i++)
            {
                dotProduct += vector1[i] * vector2[i];
            }

            double cosineDistance = dotProduct / (magnitude1 * magnitude2);

            return cosineDistance;
        }

        public List<RequestEntity> preResult { get; private set; } = new List<RequestEntity>();

        public List<RequestEntity> Result { get; private set; } = new List<RequestEntity>();

        public List<Centroid> Centroids { get; set; } = new List<Centroid>();

        public async Task GetFromDb(string request)
        {
            string connectionString = "Server=localhost; port=5432; user id=postgres; password=sa; database=WebSearchDB;";

            IDbProvider dbProvider = new PostgreDbProvider(connectionString);

            var urls = await dbProvider.GetParsedUrlsTexts();

            var glove = GloveInstance.Instance;

            double[] requestVector = await glove.GetTextVector(request);

            var coslist = new (ParsedUrl, double)[urls.Count];
            Parallel.For(0, urls.Count, i =>
            {
                coslist[i] = ((urls[i], CosDistance(requestVector, urls[i].Vector)));
            });

            var res = coslist.OrderByDescending(l => l.Item2).Take(10);

            Result.AddRange(res.Select(c =>
            {
                var entityText = c.Item1.Text;
                int index = entityText.IndexOf(request);
                int startIndex = Math.Max(0, index - 120);
                int length = Math.Min(entityText.Length - startIndex, request.Length + 240);
                return new RequestEntity()
                {
                    Url = c.Item1.URL,
                    MatchContent = entityText.Substring(startIndex, length).Replace(request, $"<strong>{request}</strong>"),
                    Tittle = c.Item1.Title,
                    Domain = "Interfax.ru"
                };
            }));

            using NpgsqlConnection con = new NpgsqlConnection(connectionString);
            var allPages = new List<ParsedUrl>();
            con.Open();

            string sql = "SELECT clusternum,vector FROM centroids";

            using NpgsqlCommand cmd = new NpgsqlCommand(sql, con);

            using NpgsqlDataReader reader = await cmd.ExecuteReaderAsync();

            while (reader.Read())
            {
                Centroid centroid = new Centroid();
                centroid.clusterNum = Convert.ToInt32(reader[0]);
                centroid.vector = centroid.vector = reader.GetFieldValue<double[]>(1);
                Centroids.Add(centroid);

            }

            double distance = 0;
            List<CentroidWithDistance> centroidWithDistances = new List<CentroidWithDistance>();

            foreach (var centroid in Centroids)
            {
                int clusterNumber = centroid.clusterNum;
                double[] centroidVector = centroid.vector;

                distance = KMeans.EuclideanDistance(requestVector, centroidVector);

                CentroidWithDistance centroidWithDistance = new CentroidWithDistance();

                centroidWithDistance.clusterNum = clusterNumber;
                centroidWithDistance.vector = centroidVector;
                centroidWithDistance.distance = distance;

                centroidWithDistances.Add(centroidWithDistance);
            }
            var nearestCentroid = centroidWithDistances.MinBy(c => c.distance);

            int nearestCluster = nearestCentroid.clusterNum;
            using NpgsqlConnection connection = new NpgsqlConnection(connectionString);
            connection.Open();
            string requestS = request;
            string finalSql = $"SELECT * FROM urlandhtml WHERE cluster = {nearestCluster}";
            NpgsqlCommand finalCmd = new NpgsqlCommand(finalSql, connection);
            NpgsqlDataReader finalReader = finalCmd.ExecuteReader();

            while (finalReader.Read())
            {
                RequestEntity newEntity = new RequestEntity();
                newEntity.Url = finalReader[0].ToString();
                string entityHtml = finalReader[1].ToString();
                string entityText = finalReader[2].ToString();

                Uri uri = new Uri(newEntity.Url);
                newEntity.Domain = uri.Host;

                newEntity.Tittle = entityHtml;
                int index = entityText.IndexOf(request);
                int startIndex = Math.Max(0, index - 120);
                int length = Math.Min(entityText.Length - startIndex, request.Length + 240);
                newEntity.MatchContent = entityText.Substring(startIndex, length).Replace(request, $"<strong>{request}</strong>"); ;


                Result.Add(newEntity);
            }

            connection.Close();
            con.Close();

        }
    }
}
