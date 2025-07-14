using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using XRCultureMiddleware.Models;
using XRCultureMiddleware.Services;

namespace XRCultureMiddleware.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TokenController : ControllerBase
    {
        private readonly ITokenService _tokenService;
        private readonly IRefreshTokenRepository _refreshTokenRepository;
        private readonly IConfiguration _configuration;

        public TokenController(
            ITokenService tokenService,
            IRefreshTokenRepository refreshTokenRepository,
            IConfiguration configuration)
        {
            _tokenService = tokenService;
            _refreshTokenRepository = refreshTokenRepository;
            _configuration = configuration;
        }

        [HttpPost("refresh")]
        public IActionResult Refresh([FromBody] TokenRefreshRequest request)
        {
            if (string.IsNullOrEmpty(request.Token) || string.IsNullOrEmpty(request.RefreshToken))
            {
                return BadRequest("Invalid token or refresh token");
            }

            var principal = _tokenService.GetPrincipalFromExpiredToken(request.Token);
            if (principal == null)
            {
                return BadRequest("Invalid token");
            }

            var jwtId = principal.Claims.First(c => c.Type == JwtRegisteredClaimNames.Jti).Value;
            var userId = principal.Claims.First(c => c.Type == JwtRegisteredClaimNames.Sub).Value;
            var username = principal.Claims.First(c => c.Type == JwtRegisteredClaimNames.Name).Value;
            var roles = principal.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value).ToList();

            var storedRefreshToken = _refreshTokenRepository.GetByToken(request.RefreshToken);
            if (storedRefreshToken == null || 
                storedRefreshToken.JwtId != jwtId || 
                storedRefreshToken.UserId != userId ||
                storedRefreshToken.ExpiryDate < DateTime.UtcNow ||
                storedRefreshToken.Used ||
                storedRefreshToken.Invalidated)
            {
                return BadRequest("Invalid refresh token");
            }

            // Mark current refresh token as used
            storedRefreshToken.Used = true;
            _refreshTokenRepository.Update(storedRefreshToken);

            // Generate new tokens
            var newToken = _tokenService.GenerateToken(userId, username, roles);
            var newJwtId = new JwtSecurityTokenHandler().ReadJwtToken(newToken).Id;
            var newRefreshToken = _tokenService.GenerateRefreshToken(userId, newJwtId);

            // Clean up old tokens
            _refreshTokenRepository.RemoveOldTokens(userId);

            return Ok(new TokenResponse
            {
                Token = newToken,
                RefreshToken = newRefreshToken.Token,
                ExpiresAt = DateTime.UtcNow.AddMinutes(60) // Match your JWT expiration
            });
        }

        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginRequest request)
        {
            if (string.IsNullOrEmpty(request.Username) || string.IsNullOrEmpty(request.Password))
            {
                return BadRequest("Username and password are required");
            }

            // Validate credentials - reuse your existing validation logic
            // This is simplified - implement your actual authentication logic
            var configUsername = _configuration["Authentication:AdminUser:Username"];
            var configPassword = _configuration["Authentication:AdminUser:Password"];
            
            bool isValidUser = request.Username == configUsername && request.Password == configPassword;
            if (!isValidUser)
            {
                return Unauthorized("Invalid username or password");
            }

            // Generate JWT token
            var token = _tokenService.GenerateToken(
                "user123", // #todo: user's ID?
                request.Username,
                new List<string> { "User" }
            );

            // Generate refresh token
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);
            var jwtId = jwtToken.Id;
            
            var refreshToken = _tokenService.GenerateRefreshToken("user123", jwtId);// #todo: user's ID?

            // Return the tokens in response
            return Ok(new TokenResponse
            {
                Token = token,
                RefreshToken = refreshToken.Token,
                ExpiresAt = DateTime.UtcNow.AddMinutes(60) // Match your JWT expiration time
            });
        }
    }

    public class TokenRefreshRequest
    {
        public string Token { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
    }

    public class TokenResponse
    {
        public string Token { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
    }

    public class LoginRequest
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}