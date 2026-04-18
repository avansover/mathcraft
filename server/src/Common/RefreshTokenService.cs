using System.Security.Cryptography;

namespace Mathcraft.Server.Common;

public interface IRefreshTokenService
{
    string GenerateToken();
    string HashToken(string token);
    void SetCookie(HttpResponse response, string token);
    void ClearCookie(HttpResponse response);
}

public class RefreshTokenService(IConfiguration configuration) : IRefreshTokenService
{
    private const string CookieName = "refreshToken";

    public string GenerateToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(64);
        return Convert.ToBase64String(bytes);
    }

    public string HashToken(string token)
    {
        var bytes = SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(token));
        return Convert.ToBase64String(bytes);
    }

    public void SetCookie(HttpResponse response, string token)
    {
        var expiryDays = configuration.GetValue<int>("Jwt:RefreshTokenExpiryDays", 30);

        response.Cookies.Append(CookieName, token, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            MaxAge = TimeSpan.FromDays(expiryDays)
        });
    }

    public void ClearCookie(HttpResponse response)
    {
        response.Cookies.Delete(CookieName, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict
        });
    }
}
