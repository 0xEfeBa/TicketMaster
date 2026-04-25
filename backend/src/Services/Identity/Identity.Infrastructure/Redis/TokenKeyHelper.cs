using System.Security.Cryptography;
using System.Text;

namespace Identity.Infrastructure.Redis;

internal static class TokenKeyHelper
{
    public static string HashRefreshToken(string plaintext)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(plaintext));
        return Convert.ToHexString(bytes);
    }
}
