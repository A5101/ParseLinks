using Parse.Domain;
using Parse.Service;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Npgsql;
using Parse.Domain.Entities;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Newtonsoft.Json.Linq;
using System.Globalization;
using System.Text.Json;

namespace JanuaryTask.Pages.Shared
{
    public class SearchPageModel : PageModel
    {
        public void OnGet()
        {

        }

        public async Task<IActionResult> OnPost(string request)
        {
            HttpClient httpClient = new HttpClient();
            var response = await httpClient.GetAsync($"https://localhost:5004/search/{request}");
            if (response.IsSuccessStatusCode)
            {
                Result.AddRange(await response.Content.ReadFromJsonAsync<IEnumerable<RequestEntity>>());
            }
            else { }
            ViewData["SearchQuery"] = request;
            USD = await GetRate("USD");
            EUR = await GetRate("EUR");
            return Page();
        }

        public async Task<double> GetRate(string valuteName)
        {
            string apiUrl = "https://www.cbr-xml-daily.ru/daily_json.js";

            using HttpClient httpClient = new HttpClient();
            string response = await httpClient.GetStringAsync(apiUrl);

            JsonDocument jsonResponse = JsonDocument.Parse(response);
            JsonElement usdElement = jsonResponse.RootElement.GetProperty("Valute").GetProperty("USD");
            JsonElement eurElement = jsonResponse.RootElement.GetProperty("Valute").GetProperty("EUR");

            if (valuteName == "USD")
            {
                if (usdElement.GetProperty("Value").TryGetDouble(out double usdRate))
                {
                    return usdRate;
                }
                else
                {
                    return 99.3026;
                }
            }
            else
            {
                if (eurElement.GetProperty("Value").TryGetDouble(out double eurRate))
                {
                    return eurRate;
                }
                else
                {
                    return 100.1873;
                }
            }

        }

        public List<RequestEntity> preResult { get; private set; } = new List<RequestEntity>();

        public List<RequestEntity> Result { get; private set; } = new List<RequestEntity>();

        public List<string> imagesFromResult { get; private set; } = new List<string>();

        public double USD { get; set; } = new double();
        public double EUR { get; set; } = new double();
    }
}
