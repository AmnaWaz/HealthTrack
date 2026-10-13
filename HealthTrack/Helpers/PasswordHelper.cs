using System;
using System.Security.Cryptography;

namespace HealthTrack.Helpers
{
    public static class PasswordHelper
    {
        private const int SaltSize = 16;
        private const int HashSize = 32;
        private const int Iterations = 10000;

        // returns base64( salt + hash )  ->  64 characters
        public static string Hash(string password)
        {
            var salt = new byte[SaltSize];
            using (var rng = new RNGCryptoServiceProvider()) rng.GetBytes(salt);

            byte[] hash;
            using (var pbkdf2 = new Rfc2898DeriveBytes(password, salt, Iterations))
                hash = pbkdf2.GetBytes(HashSize);

            var all = new byte[SaltSize + HashSize];
            Buffer.BlockCopy(salt, 0, all, 0, SaltSize);
            Buffer.BlockCopy(hash, 0, all, SaltSize, HashSize);
            return Convert.ToBase64String(all);
        }

        public static bool Verify(string password, string stored)
        {
            if (string.IsNullOrEmpty(stored)) return false;
            byte[] all;
            try { all = Convert.FromBase64String(stored); } catch { return false; }
            if (all.Length != SaltSize + HashSize) return false;

            var salt = new byte[SaltSize];
            Buffer.BlockCopy(all, 0, salt, 0, SaltSize);

            byte[] hash;
            using (var pbkdf2 = new Rfc2898DeriveBytes(password, salt, Iterations))
                hash = pbkdf2.GetBytes(HashSize);

            // constant-time compare
            var diff = 0;
            for (var i = 0; i < HashSize; i++) diff |= all[SaltSize + i] ^ hash[i];
            return diff == 0;
        }
    }
}
