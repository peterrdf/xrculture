using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using XRCultureMiddleware.Services;

namespace XRCultureMiddleware.Pages.Account
{
    [AllowAnonymous]
    public class LoginModel : PageModel
    {
        private readonly ILogger<LoginModel> _logger;
        private readonly IConfiguration _configuration;
        private readonly ITokenService _tokenService;

        public LoginModel(ILogger<LoginModel> logger, IConfiguration configuration, ITokenService tokenService)
        {
            _logger = logger;
            _configuration = configuration;
            _tokenService = tokenService;
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
                    // Generate token with user ID, name, and roles
                    var token = _tokenService.GenerateToken(
                        "user123", // #todo: user's ID?
                        LoginInput.Username,
                        ["User"]
                    );

                    var handler = new JwtSecurityTokenHandler();
                    var jwtToken = handler.ReadJwtToken(token);
                    var jwtId = jwtToken.Id;

                    var refreshToken = _tokenService.GenerateRefreshToken("user123", jwtId);// #todo: user's ID?

                    // Store both tokens
                    Response.Cookies.Append("jwt_token", token, new CookieOptions
                    {
                        HttpOnly = true,
                        Secure = true,
                        SameSite = SameSiteMode.Strict,
                        Expires = DateTime.UtcNow.AddMinutes(60) // Match your token expiry
                    });

                    Response.Cookies.Append("refresh_token", refreshToken.Token, new CookieOptions
                    {
                        HttpOnly = true,
                        Secure = true,
                        SameSite = SameSiteMode.Strict,
                        Expires = DateTime.UtcNow.AddDays(7)
                    });

                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.Name, LoginInput.Username),
                        // Add additional claims as needed
                        new Claim(ClaimTypes.Role, "User")
                    };

                    var claimsIdentity = new ClaimsIdentity(claims, "XRCultureMiddlewareCookieAuth");
                    var authProperties = new AuthenticationProperties
                    {
                        IsPersistent = LoginInput.RememberMe,
                        ExpiresUtc = DateTimeOffset.UtcNow.AddDays(14)
                    };

                    await HttpContext.SignInAsync(
                        "XRCultureMiddlewareCookieAuth",
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