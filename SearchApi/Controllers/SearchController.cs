using Microsoft.AspNetCore.Mvc;
using Npgsql;
using Parse.Domain;
using Parse.Domain.Entities;
using Parse.Service;
using Swashbuckle.AspNetCore.Annotations;

namespace SearchApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    [Produces("application/json")]
    public class SearchController : ControllerBase
    {
        private readonly IDbProvider dbProvider;

        public SearchController()
        {
            IConfiguration config = new ConfigurationBuilder()
          .SetBasePath(Directory.GetCurrentDirectory())
          .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
          .Build();

            string connectionString = config.GetConnectionString("DefaultConnection");

            dbProvider = new PostgreDbProvider(connectionString);
        }


        [HttpGet("{request}", Name = "GetSearchResults")]
        [SwaggerOperation(Summary = "Получение результатов поиска", Description = "Возвращает список результатов поиска на основе запроса.")]
        [SwaggerResponse(200, "Успешный ответ", typeof(List<RequestEntity>))]
        [SwaggerResponse(400, "Неверный запрос")]
        [SwaggerResponse(500, "Внутренняя ошибка сервера")]
        public async Task<IActionResult> GetSearchResults(string request)
        {
            if (string.IsNullOrWhiteSpace(request)) return BadRequest();
            try
            {
                var result = new List<RequestEntity>();
                var centroids = new List<Centroid>();

                var urls = await dbProvider.GetParsedUrlsTexts();

                var glove = GloveInstance.Instance;

                double[] requestVector = await glove.GetTextVector(request);

                var coslist = new (ParsedUrl, double)[urls.Count];
                Parallel.For(0, urls.Count, i =>
                {
                    coslist[i] = ((urls[i], Glove.CosDistance(requestVector, urls[i].Vector)));
                });

                var res = coslist.OrderByDescending(l => l.Item2).Take(10);

                result.AddRange(res.Select(c =>
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

                centroids.AddRange(await dbProvider.GetCentroids());

                double distance = 0;
                List<CentroidWithDistance> centroidWithDistances = new List<CentroidWithDistance>();

                foreach (var centroid in centroids)
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

                result.AddRange(await dbProvider.GetRequestEntities(nearestCentroid.clusterNum, request));

                return Ok(result);
            }
            catch
            {
                return StatusCode(500);
            }
        }


        [HttpPost("{entity}", Name = "PostIndex")]
        [SwaggerOperation(Summary = "Внос новых ссылок в базу", Description = "Добавляет в базу поисковой системы новую страницу")]
        [SwaggerResponse(201, "Успешное добавление")]
        public async Task<IActionResult> PostIndex(ApiEntity entity)
        {
            var parsedUrl = new ParsedUrl()
            {
                URL = entity.Url,
                Text = entity.Text,
                Title = entity.Title,
                DatePublished = entity.DatePublished is not null ? entity.DatePublished : DateTime.UtcNow,
                Vector = await GloveInstance.Instance.GetTextVector(entity.Text)
            };

            await dbProvider.InsertApiEntity(parsedUrl);

            return Created();
        }
    }
}
