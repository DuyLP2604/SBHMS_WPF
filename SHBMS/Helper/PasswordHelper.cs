using System.Security.Cryptography;
using System.Text;

namespace SHBMS.Helper
{
    public static class PasswordHelper
    {
        public static string Hash(string password)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(password));
            return Convert.ToHexString(bytes);
        }

        public static bool Verify(string password, string storedHash)
            => string.Equals(Hash(password), storedHash, StringComparison.OrdinalIgnoreCase);
    }
}