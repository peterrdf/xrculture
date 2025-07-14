using XRCultureMiddleware.Models;

namespace XRCultureMiddleware.Services
{
    public interface IRefreshTokenRepository
    {
        void Add(RefreshToken refreshToken);
        RefreshToken? GetByToken(string token);
        void Update(RefreshToken refreshToken);
        void RemoveOldTokens(string userId);
    }

    // This is a simple in-memory implementation
    // In a real application, use a database
    public class InMemoryRefreshTokenRepository : IRefreshTokenRepository
    {
        private readonly List<RefreshToken> _refreshTokens = new();

        public void Add(RefreshToken refreshToken)
        {
            _refreshTokens.Add(refreshToken);
        }

        public RefreshToken? GetByToken(string token)
        {
            return _refreshTokens.FirstOrDefault(t => t.Token == token);
        }

        public void Update(RefreshToken refreshToken)
        {
            var existingToken = _refreshTokens.FirstOrDefault(t => t.Token == refreshToken.Token);
            if (existingToken != null)
            {
                var index = _refreshTokens.IndexOf(existingToken);
                _refreshTokens[index] = refreshToken;
            }
        }

        public void RemoveOldTokens(string userId)
        {
            _refreshTokens.RemoveAll(t => t.UserId == userId && 
                                    (t.ExpiryDate < DateTime.UtcNow || t.Used || t.Invalidated));
        }
    }
}