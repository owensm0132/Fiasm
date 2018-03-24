using System;
using System.Collections.Generic;
using System.Text;

namespace Fiasm.Core.Interfaces.InternalInterfaces
{
    internal interface IHasher
    {
        string HashPassword(string userName, string password);
        bool VerifyPassword(string userName, string password, string hashedPassword);
    }
}
