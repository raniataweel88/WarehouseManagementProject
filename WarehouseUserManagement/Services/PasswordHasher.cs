using System.Security.Cryptography;

namespace WarehouseUserManagement.Services
{
    public interface IPasswordHasher
    {
        string HashPassword(string password);
        bool VerifyPassword(string password, string hash);
    }

    /// <summary>
    /// PBKDF2-HMAC-SHA256 password hasher. Cost is fixed regardless of password length,
    /// so verification time is O(1) with respect to input size (constant iteration count).
    /// Self-contained: no dependency on the full ASP.NET Core Identity package.
    /// Format stored: {iterations}.{saltBase64}.{hashBase64}
    /// </summary>
    public class PasswordHasher : IPasswordHasher
    {
        private const int SaltSize = 16;      // 128-bit
        private const int KeySize = 32;       // 256-bit
        private const int Iterations = 100_000;
        private static readonly HashAlgorithmName Algorithm = HashAlgorithmName.SHA256;

        public string HashPassword(string password)
        {
            var salt = RandomNumberGenerator.GetBytes(SaltSize);
            var key = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, Algorithm, KeySize);
            return $"{Iterations}.{Convert.ToBase64String(salt)}.{Convert.ToBase64String(key)}";
        }

        public bool VerifyPassword(string password, string hash)
        {
            var parts = hash.Split('.', 3);
            if (parts.Length != 3) return false;

            if (!int.TryParse(parts[0], out var iterations)) return false;
            var salt = Convert.FromBase64String(parts[1]);
            var expectedKey = Convert.FromBase64String(parts[2]);

            var actualKey = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, Algorithm, expectedKey.Length);

            // Fixed-time comparison to avoid timing attacks
            return CryptographicOperations.FixedTimeEquals(actualKey, expectedKey);
        }
    }
}
