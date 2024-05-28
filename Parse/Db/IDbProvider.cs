using Npgsql;
using Parse.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Parse.Domain
{
    public interface IDbProvider
    {
        Task<string> GetAnotherUrlsCount();

        Task InsertParsedUrl(ParsedUrl parsedUrl);

        Task InsertUrlQueue(string url);

        Task InsertUrlQueue(List<string> url);

        Task InsertImages(List<Image> image);

        Task<List<Domen>> InsertDomen(Domen robots);

        Task<List<Domen>> GetRobots();

        Task UpdateDomen(Domen domen);

        Task<List<ParsedUrl>> GetParsedUrlsTexts();

        Task InsertAnotherLink(List<string> urls);

        Task ClearAnotherUrls();

        Task<List<string>> GetAnotherUrls();

        Task DeleteAnotherUrl(string url);

        Task<bool> ContainsUrlandhtml(string url);

        Task<bool> ContainsUnaccessedUrll(string url);

        Task InsertUnaccessedUrl(string url);

        Task Truncate();

        Task<List<RequestEntity>> GetRequestEntities(int clusterNum, string request);

        Task<List<Centroid>> GetCentroids();

        Task InsertApiEntity(ParsedUrl parsedUrl);
    }
}
