using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace XRCultureWebApp.Pages
{
    [Authorize]
    public class LibraryModel : PageModel
    {
        public void OnGet()
        {
        }
    }
}
