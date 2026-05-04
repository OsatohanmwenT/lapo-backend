using System.Security.Cryptography;
using System.Text;

namespace lapo_vms_api.Helpers;

public static class AuditHelper
{
    public static string ComputeHash(string input)
    {
        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(input);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToHexString(hash);
    }
}
