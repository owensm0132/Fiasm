using Fiasm.Core.ServiceInterfaces;
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Identity;
namespace Fiasm.Core.Services
{
    public class Hasher : IHasher
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
