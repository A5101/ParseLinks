using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Npgsql;

namespace JanuaryTask.Pages.Shared
{

    public class SearchPageModel : PageModel
    {
 
        public void OnGet()
        {
        }

        public IActionResult OnPost(string request)
        {
            return RedirectToPage("searchresult",new { requestString = request });
        }
    } 
}
