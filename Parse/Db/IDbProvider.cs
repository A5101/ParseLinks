using Npgsql;
using Parse.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Parse.Domain
{
    internal interface IDbProvider
    {
        Task<string> GetAnotherUrlsCount();

        Task InsertParsedUrl(ParsedUrl parsedUrl);

        Task<List<Domen>> InsertDomen(Domen robots);

        Task<List<Domen>> GetRobots();
        Task<List<ParsedUrl>> GetParsedUrlsTexts();

        Task InsertAnotherLink(List<string> urls);

        Task ClearAnotherUrls();

        Task<List<string>> GetAnotherUrls();

        Task DeleteAnotherUrl(string url);

        Task<bool> ContainsUrlandhtml(string url);

        Task<bool> ContainsUnaccessedUrll(string url);

        Task InsertUnaccessedUrl(string url);

        Task Truncate();

    }
}
