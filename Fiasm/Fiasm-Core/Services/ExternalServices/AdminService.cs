using Fiasm.Core.Interfaces.ExternalInterfaces;
using Fiasm.Core.Interfaces.InternalInterfaces;
using Fiasm.Core.Models.RequestModels;
using Fiasm.Core.Models.UserModels;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Security.Principal;
using System.Text;
using System.Linq;
using Fiasm.Repository;
using Fiasm.Repository.EntityModels;

namespace Fiasm.Core.Services.ExternalServices
{
    public class AdminService : IAdminService
    {
        private IUserService userService;

        public AdminService(IUserService userService) => this.userService = userService ??
            throw new ArgumentNullException(nameof(userService));

        public UserModel AuthenticateUser(LoginModel login)
        {
            if (login == null) throw new ArgumentNullException(nameof(login));

            return userService.AuthenticateUserAsync(login).Result;
        }

        public ResponseModel CreateNewUser(UserModel currentUser, UserModel newUser)
        {
            if (currentUser == null) throw new ArgumentNullException(nameof(currentUser));
            if (newUser == null) throw new ArgumentNullException(nameof(newUser));

            var response = new ResponseModel{ Success = false, ErrorMessage = "Not authorized" };

            if (currentUser.Claims.Any(c =>
                c.ClaimType == ClaimTypes.AuthorizedToDoUserAdministration.ToString()))
            {
                try
                {
                    using (var db = new FiasmDbContext())
                    {
                        db.AppUsers.Add(new AppUser
                        {
                            AppUserClaims = newUser.Claims.Select(c => new AppUserClaim
                            {
                                AppUserClaimValue = c.ClaimValue,
                                AppClaim = new AppClaimdb.AppClaims.Single(ac => ac.ClaimType == c.ClaimType).ClaimType,
                            })
                        });

                    }
                }
                catch (Exception exception)
                {

                    throw;
                }
            }

            return response;
        }

        public ResponseModel DeleteUser(UserModel currentUser, UserModel deletedUser)
        {
            if (currentUser == null) throw new ArgumentNullException(nameof(currentUser));
            if (deletedUser == null) throw new ArgumentNullException(nameof(deletedUser));

            throw new NotImplementedException();
        }

        public IEnumerable<UserModel> GetUsers(UserModel currentUser)
        {
            if (currentUser == null) throw new ArgumentNullException(nameof(currentUser));

            throw new NotImplementedException();
        }

        public ResponseModel UpdateUser(UserModel currentUser, UserModel modifiedUser)
        {
            if (currentUser == null) throw new ArgumentNullException(nameof(currentUser));
            if (modifiedUser == null) throw new ArgumentNullException(nameof(modifiedUser));

            throw new NotImplementedException();
        }
    }
}
