using Fiasm.Core.Models.UserModels;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Security.Authentication;

namespace Fiasm.Core.Utilities
{
    public class FiasmErrorHandling
    {
        public static void VerifyArgNotNull<Type>(Type arg)
        {
            if (arg == null) throw new ArgumentNullException(nameof(arg));
        }

        public static void VerifyUserPermission(Fiasm.Data.EntityModels.User user, ClaimTypes claimType )
        {
            if (user == null) throw new ArgumentNullException(nameof(user));

            if (!user.UserClaims.Any(c => c.Claim.ClaimType == claimType.ToString()))
            {
                throw new InvalidCredentialException("You do not have permission.");
            }
        }
    }
}
