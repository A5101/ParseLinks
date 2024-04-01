using JanuaryTask.Pages.Shared.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Npgsql;
using System.Text.RegularExpressions;

namespace JanuaryTask.Pages.Shared
{
    public class SearchResultModel : PageModel
    {
        public void OnGet(string requestString)
        {
            requestString = requestString;
            Request = requestString;
            GetFromDb(requestString);
        }
        public string Request {  get; set; }
        public List<RequestEntity> Result { get; private set; } = new List<RequestEntity>();

        public async void GetFromDb(string request)
        {
            string connectionString = "Server=localhost; port=5432; user id=postgres; password=sa; database=60k;";
            NpgsqlConnection connection = new NpgsqlConnection(connectionString);
            connection.Open();
            string requestS = request;
            string sql = $"SELECT * FROM urlandhtml WHERE text LIKE '%{requestS}%' limit 10";
            NpgsqlCommand cmd = new NpgsqlCommand(sql, connection);
            NpgsqlDataReader reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                RequestEntity newEntity = new RequestEntity();
                newEntity.Url = reader[0].ToString();
                string entityHtml = reader[1].ToString();
                string entityText = reader[2].ToString();

                newEntity.Tittle = entityHtml;
                int index = entityText.IndexOf(request);
                int startIndex = Math.Max(0, index - 120);
                int length = Math.Min(entityText.Length - startIndex, request.Length + 240);
                newEntity.MatchContent = entityText.Substring(startIndex, length).Replace(request, $"<strong>{request}</strong>"); ;


                Result.Add(newEntity);
            }
        }
    }
}
