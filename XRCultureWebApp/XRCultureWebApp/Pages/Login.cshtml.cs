using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace XRCultureWebApp.Pages
{
    [AllowAnonymous]
    public class LoginModel : PageModel
    {
        private readonly ILogger<LoginModel> _logger;

        private readonly IHttpContextAccessor _httpContextAccessor;
        public LoginModel(ILogger<LoginModel> logger, IHttpContextAccessor httpContextAccessor)
        {
            _logger = logger;
            _httpContextAccessor = httpContextAccessor;
        }

        public class InputModel
        {
            [Required]
            [EmailAddress]
            public string Email { get; set; }

            [Required]
            [DataType(DataType.Password)]
            public string Password { get; set; }
        }

        [BindProperty]
        public InputModel Input { get; set; }

        [BindProperty]
        public string Username { get; set; }
        [BindProperty]
        public string Password { get; set; }

        public string ErrorMessage { get; set; }
        [BindProperty(SupportsGet = true)]
        public string ReturnUrl { get; set; }

        public async Task OnGetAsync(string returnUrl = null)
        {
            if (!string.IsNullOrEmpty(ErrorMessage))
            {
                ModelState.AddModelError(string.Empty, ErrorMessage);
            }
            //returnUrl = "/Index";
            returnUrl = returnUrl ?? Url.Content("/Index");
        }



        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            _logger.LogInformation(" OnPostAsync()");
            if (string.IsNullOrEmpty(Username) || string.IsNullOrEmpty(Password))
            {
                ErrorMessage = "Username and password are required";
                return Page();
            }
            // Hardcoded credentials
            if (Username == "q" && Password == "q")
            {
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, Username)
                };
                var identity = new ClaimsIdentity(claims, "MyCookieAuth");
                var principal = new ClaimsPrincipal(identity);

                await HttpContext.SignInAsync("MyCookieAuth", principal);

                returnUrl = returnUrl ?? Url.Content("/Index");

                return LocalRedirect("/Index");// RedirectToPage("/Index");
            }

            ErrorMessage = "Invalid username or password";
            return Page();
        }


    }
}
