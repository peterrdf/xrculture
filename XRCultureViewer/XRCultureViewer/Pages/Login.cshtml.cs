using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace XRCultureViewer.Pages
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

            ReturnUrl = returnUrl ?? Url.Content("~/Index");
        }

        public async Task<IActionResult> OnPostAsync()
        {
            _logger.LogInformation(" OnPostAsync()");
            if (string.IsNullOrEmpty(Username) || string.IsNullOrEmpty(Password))
            {
                ErrorMessage = "User and Password are required.";
                return Page();
            }
            if (Username == "xrculture" && Password == "Q7!vRz2#pLw8@tXb")
            {
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, Username)
                };
                var identity = new ClaimsIdentity(claims, "XRCultureCookieAuth");
                var principal = new ClaimsPrincipal(identity);

                await HttpContext.SignInAsync("XRCultureCookieAuth", principal);

                if (string.IsNullOrEmpty(ReturnUrl) || !Url.IsLocalUrl(ReturnUrl))
                {
                    return RedirectToPage("/Index");
                }
                else
                {
                    return Redirect(ReturnUrl);
                }
            }

            ErrorMessage = "Invalid User or Password";

            return Page();
        }
    }
}
