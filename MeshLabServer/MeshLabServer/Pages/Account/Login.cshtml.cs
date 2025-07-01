using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MeshLabServer.Pages.Account
{
    [AllowAnonymous]
    public class LoginModel : PageModel
    {
        private readonly ILogger<LoginModel> _logger;
        private readonly IConfiguration _configuration;

        public LoginModel(ILogger<LoginModel> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }

        [BindProperty]
        public LoginInputModel LoginInput { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public string ReturnUrl { get; set; } = "/";

        public string? ErrorMessage { get; set; }

        public void OnGet(string returnUrl = "")
        {
            if (!string.IsNullOrEmpty(ErrorMessage))
            {
                ModelState.AddModelError(string.Empty, ErrorMessage);
            }

            ReturnUrl = returnUrl ?? Url.Content("~/");
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (ModelState.IsValid && !string.IsNullOrEmpty(LoginInput.Username) && !string.IsNullOrEmpty(LoginInput.Password))
            {
                // Replace this with your actual authentication logic
                if (IsValidUser(LoginInput.Username, LoginInput.Password))
                {
                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.Name, LoginInput.Username),
                        // Add additional claims as needed
                        new Claim(ClaimTypes.Role, "User")
                    };

                    var claimsIdentity = new ClaimsIdentity(claims, "MeshLabServerCookieAuth");
                    var authProperties = new AuthenticationProperties
                    {
                        IsPersistent = LoginInput.RememberMe,
                        ExpiresUtc = DateTimeOffset.UtcNow.AddDays(14)
                    };

                    await HttpContext.SignInAsync(
                        "MeshLabServerCookieAuth",
                        new ClaimsPrincipal(claimsIdentity),
                        authProperties);

                    _logger.LogInformation("User {Username} logged in at {Time}.",
                        LoginInput.Username, DateTime.UtcNow);

                    if (Url.IsLocalUrl(ReturnUrl))
                    {
                        return LocalRedirect(ReturnUrl);
                    }

                    return RedirectToPage("/Index");
                }

                ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                return Page();
            }

            // If we got this far, something failed, redisplay form
            return Page();
        }

        // Replace this with your actual user validation logic
        private bool IsValidUser(string? username, string? password)
        {
            // For demo purposes #todo: Implement your user validation logic here.
            // Options:
            // 1. Check against users in a database
            // 2. Use ASP.NET Core Identity
            // 3. Check against configured users in appsettings.json

            var configUsername = _configuration["Authentication:AdminUser:Username"];
            var configPassword = _configuration["Authentication:AdminUser:Password"];

            return username == configUsername && password == configPassword;
        }
    }

    public class LoginInputModel
    {
        [Required]
        public string? Username { get; set; }

        [Required]
        [DataType(DataType.Password)]
        public string? Password { get; set; }

        [Display(Name = "Remember me")]
        public bool RememberMe { get; set; }
    }
}