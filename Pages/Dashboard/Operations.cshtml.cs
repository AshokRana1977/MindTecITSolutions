using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;

namespace MindtecFinance.Pages.Dashboard
{
    [Authorize]
    public class OperationsModel : PageModel
    {
        public void OnGet()
        {
        }
    }
}
