using System.Security.Cryptography;

namespace SampleApp.Auth;

public static class TokenAuthenticator
{
    public static bool IsAdmin(string token)
    {
        if (string.IsNullOrEmpty(token))
            return false;

        string? adminToken = Environment.GetEnvironmentVariable("ADMIN_TOKEN");
        if (string.IsNullOrEmpty(adminToken))
            return false;

        return CryptographicOperations.FixedTimeEquals(
            System.Text.Encoding.UTF8.GetBytes(token),
            System.Text.Encoding.UTF8.GetBytes(adminToken));
    }
}
