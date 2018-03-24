using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using System.Security.Principal;
using System.Security.Claims;
using Fiasm.Core.Models.UserModels;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using Fiasm.Repository.EntityModels;
using Fiasm.Core.Interfaces.InternalInterfaces;

namespace Fiasm.Core.Services
{
    internal class UserService : IUserService
    {
        private IHasher hasher;

        public UserService(IHasher hasher)
        {
            this.hasher = hasher;
        }

        #region authentication
        public async Task<UserModel> AuthenticateUserAsync(LoginModel login)
        {
            UserModel user = null;
            using (var db = new Fiasm.Repository.FiasmDbContext())
            {
                var dbUser = await db.AppUsers.FirstOrDefaultAsync(u => u.LoginName == login.LoginName);
                if(dbUser != null && hasher.VerifyPassword(
                    login.LoginName, login.PassWord, dbUser.HashedPassword))
                {
                    user =  new UserModel
                    {
                        LoginName = login.LoginName
                    };
                }
            }
            return user;
        }
        #endregion

        #region claims
        public async Task<bool> DoesUserHaveClaimAsync(UserModel user, string claimType)
        {
            using (var db = new Fiasm.Repository.FiasmDbContext())
            {
                var dbUser = await db.AppUsers.FirstOrDefaultAsync(u => u.LoginName == user.LoginName);
                return dbUser?.AppUserClaims.Any( c => c.AppClaim.ClaimType == claimType) ?? false;
            }
        }

        public async Task<string> GetUserClaimValueAsync(UserModel user, string claimType)
        {
            using (var db = new Fiasm.Repository.FiasmDbContext())
            {
                var dbUser = await db.AppUsers.FirstOrDefaultAsync(u => u.LoginName == user.LoginName);
                return dbUser?.AppUserClaims.FirstOrDefault(c => c.AppClaim.ClaimType == claimType)
                    ?.AppUserClaimValue;
            }
        }

        public async Task<IEnumerable<ClaimModel>> GetUserClaimsAsync(UserModel user)
        {
            using (var db = new Fiasm.Repository.FiasmDbContext())
            {
                var dbUser = await db.AppUsers.FirstOrDefaultAsync(u => u.LoginName == user.LoginName);
                return dbUser?.AppUserClaims.Select(auc => new ClaimModel
                {
                    ClaimType = auc.AppClaim.ClaimType,
                    ClaimValue = auc.AppUserClaimValue
                }).ToList();   
            }
        }

        public async Task AddUserClaim(UserModel user, ClaimModel claim)
        {
            if (user == null) throw new ArgumentNullException("user");
            if (claim == null) throw new ArgumentNullException("claim");

            using (var db = new Fiasm.Repository.FiasmDbContext())
            {
                var dbUser = await db.AppUsers.FirstOrDefaultAsync(u => u.LoginName == user.LoginName);
                if (dbUser == null)
                {
                    throw new Exception("User not found");
                }

                var dbClaim = await db.AppClaims.FirstOrDefaultAsync(c => c.ClaimType == claim.ClaimType);
                if(dbClaim == null)
                {
                    throw new Exception("Claim not found");
                }

                if(dbUser.AppUserClaims.Any(auc => auc.AppClaim.ClaimType == claim.ClaimType))
                {
                    throw new Exception($"User already has claim type '{claim.ClaimType}'");
                }

                dbUser.AppUserClaims.Add(new AppUserClaim
                {
                    AppClaim = dbClaim,
                    AppUserClaimValue = claim.ClaimValue
                });
                await db.SaveChangesAsync();
            }
        }

        public async Task AddClaimType(ClaimModel claim)
        {
            if (claim == null) throw new ArgumentNullException("claim");

            using (var db = new Fiasm.Repository.FiasmDbContext())
            {
                if (db.AppClaims.Any(c => c.ClaimType == claim.ClaimType))
                {
                    throw new Exception($"Claim type '{claim.ClaimType}' already exists");
                }
                db.AppClaims.Add(new AppClaim
                {
                    ClaimType = claim.ClaimType
                });
                await db.SaveChangesAsync();
            }
        }

        public async Task<bool> SafeDeleteClaim(ClaimModel claim)
        {
            if (claim == null) throw new ArgumentNullException("claim");

            bool successful = false;
            using (var db = new Fiasm.Repository.FiasmDbContext())
            {
                if (!db.AppClaims.Any(c => c.ClaimType == claim.ClaimType))
                {
                    throw new Exception($"Claim type '{claim.ClaimType}' does not exist");
                }
                if(!db.AppUsers.Any(u => u.AppUserClaims.Any(c => c.AppClaim.ClaimType == claim.ClaimType)))
                {
                    // it is safe to delete this claim because no users have this claim
                    db.AppClaims.Remove(new AppClaim
                    {
                        ClaimType = claim.ClaimType
                    });
                    await db.SaveChangesAsync();
                    successful = true;
                }
            }
            return successful;
        }
        public async Task ForceDeleteClaim(ClaimModel claim)
        {
            if (claim == null) throw new ArgumentNullException("claim");

            using (var db = new Fiasm.Repository.FiasmDbContext())
            {
                if (!db.AppClaims.Any(c => c.ClaimType == claim.ClaimType))
                {
                    throw new Exception($"Claim type '{claim.ClaimType}' does not exist");
                }
                var usersWithClaim = db.AppUsers
                    .Where(u => u.AppUserClaims.Any(c => c.AppClaim.ClaimType == claim.ClaimType));
                foreach (var user in usersWithClaim)
                {
                    // it is not possible for a single user to have more than one claim with the same claim value
                    user.AppUserClaims.Remove(user.AppUserClaims
                        .Single(auc => auc.AppClaim.ClaimType == claim.ClaimType));
                }
                db.AppClaims.Add(new AppClaim
                {
                    ClaimType = claim.ClaimType
                });
                await db.SaveChangesAsync();
            }
        }


        public async Task RemoveUserClaim(UserModel user, ClaimModel claim)
        {
            if (user == null) throw new ArgumentNullException("user");
            if (claim == null) throw new ArgumentNullException("claim");

            using (var db = new Fiasm.Repository.FiasmDbContext())
            {
                var dbUser = await db.AppUsers.FirstOrDefaultAsync(u => u.LoginName == user.LoginName);
                if (dbUser == null) throw new Exception($"Could not find user '{user.LoginName}'");
                var dbClaim = await db.AppClaims.FirstOrDefaultAsync(c => c.ClaimType == claim.ClaimType);
                if (dbClaim == null) throw new Exception($"Could not find claim type '{claim.ClaimType}'");

                dbUser.AppUserClaims.Add(new AppUserClaim
                {
                    AppClaim = dbClaim,
                    AppUserClaimValue = claim.ClaimValue
                });
                
                await db.SaveChangesAsync();
            }
        }

        public async Task UpdateUserClaimValue(UserModel user, ClaimModel claim)
        {
            if (user == null) throw new ArgumentNullException("user");
            if (claim == null) throw new ArgumentNullException("claim");

            using (var db = new Fiasm.Repository.FiasmDbContext())
            {
                var dbUser = await db.AppUsers.FirstOrDefaultAsync(u =>
                    u.LoginName == user.LoginName &&
                    u.AppUserClaims.Any(auc => auc.AppClaim.ClaimType == claim.ClaimType));
                if (dbUser == null) throw new Exception($"Could not find user '{user.LoginName}' with claim type '{claim.ClaimType}'");

                dbUser.AppUserClaims.Single(auc => auc.AppClaim.ClaimType == claim.ClaimType)
                    .AppUserClaimValue = claim.ClaimValue;
                
                await db.SaveChangesAsync();
            }
        }
        #endregion

        #region accounts
        public async Task AddUserAsync(UserModel user, string password)
        {
            if (user == null) throw new ArgumentNullException("user");
            var hashedPassword = hasher.HashPassword(user.LoginName, password);

            using (var db = new Fiasm.Repository.FiasmDbContext())
            {
                if(db.AppUsers.Any(u => u.LoginName == user.LoginName))
                {
                    throw new Exception($"User with login name '{user.LoginName}' already exists");
                }
                db.AppUsers.Add(new AppUser
                {
                    LoginName = user.LoginName,
                    Email = user.EmailAddress,
                    Phone = user.PhoneNumber,
                    HashedPassword = hashedPassword
                });

                await db.SaveChangesAsync();
            }
        }

        public async Task DeleteUserAsync(UserModel user)
        {
            if (user == null) throw new ArgumentNullException("user");

            using (var db = new Fiasm.Repository.FiasmDbContext())
            {
                var dbUser = await db.AppUsers.FirstOrDefaultAsync(u => u.LoginName == user.LoginName);
                if (dbUser == null)
                {
                    throw new Exception($"User '{user.LoginName}' not found");
                }
                db.AppUsers.Remove(dbUser);

                await db.SaveChangesAsync();
            }
        }
 
        public async Task DeactivateUserAsync(UserModel user)
        {
            if (user == null) throw new ArgumentNullException("user");

            using (var db = new Fiasm.Repository.FiasmDbContext())
            {
                var dbUser = await db.AppUsers.FirstOrDefaultAsync(u => u.LoginName == user.LoginName);
                if (dbUser == null)
                {
                    throw new Exception($"User '{user.LoginName}' not found");
                }
                if (dbUser.IsActive)
                {
                    dbUser.IsActive = false;
                    await db.SaveChangesAsync();
                }
            }
        }

        public async Task ActivateUserAsync(UserModel user)
        {
            if (user == null) throw new ArgumentNullException("user");

            using (var db = new Fiasm.Repository.FiasmDbContext())
            {
                var dbUser = await db.AppUsers.FirstOrDefaultAsync(u => u.LoginName == user.LoginName);
                if (dbUser == null)
                {
                    throw new Exception($"User '{user.LoginName}' not found");
                }
                if (!dbUser.IsActive)
                {
                    dbUser.IsActive = true;
                    await db.SaveChangesAsync();
                }
            }
        }

        public Task AddUserAsync(UserModel user)
        {
            throw new NotImplementedException();
        }
        #endregion
    }
}
