using System;
using System.Security.Cryptography;
using System.Text;

namespace IPCSoftware.Services
{
    public static class SecurityService
    {
        // HARDCODED APPLICATION SECRET. 
        // In a real high-security scenario, this should be obfuscated or stored in Windows Credential Manager.
        // For preventing CSV edits, this is sufficient.
        private static readonly byte[] AppSecretKey = Encoding.UTF8.GetBytes("YOUR-SUPER-SECRET-GUID-KEY-HERE-9999");

        // 1. Hash Password (PBKDF2)
        public static string HashPassword(string password, out string salt)
        {
            var saltBytes = new byte[16];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(saltBytes);
            }
            salt = Convert.ToBase64String(saltBytes);

            return ComputeHash(password, saltBytes);
        }

        public static bool VerifyPassword(string password, string storedHash, string storedSalt)
        {
            var saltBytes = Convert.FromBase64String(storedSalt);
            var computedHash = ComputeHash(password, saltBytes);
            return computedHash == storedHash;
        }

        private static string ComputeHash(string password, byte[] salt)
        {
            using (var pbkdf2 = new Rfc2898DeriveBytes(password, salt, 10000, HashAlgorithmName.SHA256))
            {
                byte[] hash = pbkdf2.GetBytes(32);
                return Convert.ToBase64String(hash);
            }
        }

        // 2. Generate Row Signature (HMAC)
        // This binds the Role to the Username. You cannot copy the role to another user 
        // because the Username is part of the math!
        public static string GenerateRowSignature(int id, string username, string role, string passwordHash, bool isActive)
        {
            // Combine all sensitive fields
            string payload = $"{id}|{username.ToLower()}|{role}|{passwordHash}|{isActive}";

            using (var hmac = new HMACSHA256(AppSecretKey))
            {
                byte[] signatureBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
                return Convert.ToBase64String(signatureBytes);
            }
        }
    }
}