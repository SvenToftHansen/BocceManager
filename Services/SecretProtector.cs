using System.Security.Cryptography;
using System.Text;

namespace BocceManager.Services;

// Encrypts small secrets (OAuth client secret, refresh token) at rest using Windows DPAPI,
// scoped to the current Windows user, before they're stored in the AppParameters table.
public static class SecretProtector
{
    public static string Protect(string plainText)
    {
        var bytes = Encoding.UTF8.GetBytes(plainText);
        var protectedBytes = ProtectedData.Protect(bytes, null, DataProtectionScope.CurrentUser);
        return Convert.ToBase64String(protectedBytes);
    }

    public static string Unprotect(string protectedText)
    {
        var protectedBytes = Convert.FromBase64String(protectedText);
        var bytes = ProtectedData.Unprotect(protectedBytes, null, DataProtectionScope.CurrentUser);
        return Encoding.UTF8.GetString(bytes);
    }
}
