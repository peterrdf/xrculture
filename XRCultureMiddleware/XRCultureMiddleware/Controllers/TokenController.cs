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

        public TokenController(
            ITokenService tokenService,
            IRefreshTokenRepository refreshTokenRepository)
        {
            _tokenService = tokenService;
            _refreshTokenRepository = refreshTokenRepository;
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
}