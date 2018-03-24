using Microsoft.AspNetCore.Identity;
using Fiasm.Core.Interfaces.InternalInterfaces;

namespace Fiasm.Core.Services
{
    internal class Hasher : IHasher
    {
        public string HashPassword(string userName, string password)
        {
            var hasher = new PasswordHasher<string>();
            return hasher.HashPassword(userName, password);
        }

        public bool VerifyPassword(string userName, string password, string hashedPassword)
        {
            var hasher = new PasswordHasher<string>();
            return hasher.VerifyHashedPassword(userName, hashedPassword, password) == PasswordVerificationResult.Success;
        }
    }
}
