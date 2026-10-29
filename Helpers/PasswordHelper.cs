using System.Security.Cryptography;
using System.Text;

namespace WebsiteBanDienThoai.Helpers
{
    public static class PasswordHelper
    {
        private const string Salt = "CellPhoneS_Auth_Secret_Salt_2026";

        public static string HashPassword(string password)
        {
            if (string.IsNullOrEmpty(password)) return string.Empty;

            using var sha256 = SHA256.Create();
            var saltedPassword = password + Salt;
            var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(saltedPassword));
            return Convert.ToBase64String(bytes);
        }

        public static bool VerifyPassword(string password, string storedHash)
        {
            if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(storedHash)) return false;

            var computedHash = HashPassword(password);
            return computedHash == storedHash;
        }
    }
}
