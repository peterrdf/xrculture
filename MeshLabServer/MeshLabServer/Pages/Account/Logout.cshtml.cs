using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MeshLabServer.Pages.Account
{
    public class LogoutModel : PageModel
    {
        public async Task<IActionResult> OnGetAsync()
        {
            // Sign out the user with the correct scheme
            await HttpContext.SignOutAsync("MeshLabServerCookieAuth");

            // Redirect to home page after logout
            return RedirectToPage("/Index");
        }

        public async Task<IActionResult> OnPostAsync()
        {
            // Also handle POST requests for logout
            return await OnGetAsync();
        }
    }
}