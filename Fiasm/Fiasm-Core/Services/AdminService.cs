using Fiasm.Core.Interfaces.ExternalInterfaces;
using Fiasm.Core.Interfaces.InternalInterfaces;
using Fiasm.Core.Models.RequestModels;
using Fiasm.Core.Models.UserModels;
using System;
using System.Collections.Generic;
using System.Security.Principal;
using System.Text;
using System.Linq;
using Fiasm.Data;
using Fiasm.Data.EntityModels;
using Fiasm.Core.Utilities;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using AutoMapper;

namespace Fiasm.Core.Services.ExternalServices
{
    public class AdminService : IAdminService
    {
        private readonly FiasmDbContext dbContext;
        private readonly IHasher hasher;
        private IMapper mapper;

        #region Private Functions

        private async Task<User> GetDbUserFromUserModelByUserNameAsync(UserModel userModel)
        {
            FiasmErrorHandling.VerifyArgNotNull(userModel);
            return await dbContext.Users.SingleOrDefaultAsync(u => u.UserName == userModel.UserName);
        }

        private async Task<User> GetDbUserFromUserModelByEmailAsync(UserModel userModel)
        {
            FiasmErrorHandling.VerifyArgNotNull(userModel);
            return await dbContext.Users.SingleOrDefaultAsync(u => u.EmailAddress == userModel.EmailAddress);
        }
        
        private async Task<User> GetLoggedInUserFromUserModelAsync(UserModel userModel)
        {
            var dbUser = await GetDbUserFromUserModelByUserNameAsync(userModel);
            if (dbUser == null)
                throw new ArgumentException($"Could not find logged in user '{userModel.UserName}'.");
            return dbUser;
        }
        #endregion

        public AdminService(FiasmDbContext dbContext, IHasher hasher)
        {
            this.dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));

            mapper = new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<User, UserModel>();
                cfg.CreateMap<UserModel, User>()
                    .ForMember(dest => dest.UserClaims, opt => opt.Ignore())
                    .ForMember(dest => dest.HashedPassword, opt => opt.Ignore())
                    .ForMember(dest => dest.IsPasswordReseting, opt => opt.Ignore())
                    .ForMember(dest => dest.IsPasswordResetRequired, opt => opt.Ignore())
                    .ForMember(dest => dest.ResettingPasswordUUID, opt => opt.Ignore())
                    .ForMember(dest => dest.UserId, opt => opt.Ignore());
                cfg.CreateMap<Claim, ClaimModel>();
                cfg.CreateMap<ClaimModel, Claim>()
                    .ForMember(dest => dest.UserClaims, opt => opt.Ignore())
                    .ForMember(dest => dest.ClaimId, opt => opt.Ignore());
            }).CreateMapper() ;

            this.hasher = hasher;
        }

        public async Task<UserModel> AuthenticateUserAsync(LoginModel login)
        {
            FiasmErrorHandling.VerifyArgNotNull(login);

            UserModel user = null;

            var dbUser = await dbContext.Users.FirstOrDefaultAsync(u => u.UserName == login.LoginName);
            if (dbUser != null && hasher.VerifyPassword(
                login.LoginName, login.PassWord, dbUser.HashedPassword))
            {
                user = new UserModel
                {
                    UserName = login.LoginName
                };
            }
            return user;
        }

        public async Task<IEnumerable<UserModel>> GetUsersAsync(UserModel loggedInUser)
        {
            FiasmErrorHandling.VerifyArgNotNull(loggedInUser);

            var currentUser = await GetLoggedInUserFromUserModelAsync(loggedInUser);

            var dbUsers = await dbContext.Users.ToListAsync();
            return mapper.Map<IList<User>, IList<UserModel>>(dbUsers);
        }

        public async Task<ResponseModel> UpdateUserAsync(UserModel loggedInUser, UserModel modifiedUser)
        {
            FiasmErrorHandling.VerifyArgNotNull(loggedInUser);
            FiasmErrorHandling.VerifyArgNotNull(modifiedUser);

            var currentUser = await GetLoggedInUserFromUserModelAsync(loggedInUser);
            if(currentUser == null)
                throw new ArgumentException($"Could not find logged");

            // if the current user is modifying another user, check the permissions
            if(loggedInUser.UserName != modifiedUser.UserName)
                FiasmErrorHandling.VerifyUserPermission(currentUser, ClaimTypes.AuthorizedToDoUserAdministration);

            var response = new ResponseModel { Success = false };

            var dbModifiedUser = await GetDbUserFromUserModelByUserNameAsync(modifiedUser);
            if(dbModifiedUser == null)
            {
                response.ErrorMessage = $"Could not find user '{modifiedUser.UserName}'";
            }
            else
            {
                var changeDesc = "";
                if (dbModifiedUser.PhoneNumber != modifiedUser.PhoneNumber)
                {
                    changeDesc += $"Changed PhoneNumber from {dbModifiedUser.PhoneNumber} to {modifiedUser.PhoneNumber}.";
                    dbModifiedUser.PhoneNumber = modifiedUser.PhoneNumber;
                }
                if (dbModifiedUser.EmailAddress != modifiedUser.EmailAddress)
                {
                    changeDesc += $"Changed PhoneNumber from {dbModifiedUser.EmailAddress} to {modifiedUser.EmailAddress}.";
                    dbModifiedUser.EmailAddress = modifiedUser.EmailAddress;
                }
                if (string.IsNullOrEmpty(changeDesc))
                {
                    response.ErrorMessage = $"No user settings were different for user '{modifiedUser.UserName}'";
                }
                else
                {
                    dbContext.UserChangeLogs.Add(new UserChangeLog
                    {
                        ChangedByUserId = currentUser.UserId,
                        ChangeDesc = $"Update user ; UserId = {dbModifiedUser.UserId}. {changeDesc}",
                        ModifiedOn = DateTime.Now,
                        ChangedUserId = currentUser.UserId
                    });
                    await dbContext.SaveChangesAsync();
                    response = new ResponseModel { Success = true };
                }
            }
            
            return response;
        }

        public async Task<ResponseModel> ActivateUserAsync(UserModel loggedInUser, UserModel userToActivate)
        {
            FiasmErrorHandling.VerifyArgNotNull(loggedInUser);
            FiasmErrorHandling.VerifyArgNotNull(userToActivate);

            var currentUser = await GetLoggedInUserFromUserModelAsync(loggedInUser);

            FiasmErrorHandling.VerifyUserPermission(currentUser, ClaimTypes.AuthorizedToDoUserAdministration);

            var response = new ResponseModel { Success = false };

            var dbModifiedUser = await GetDbUserFromUserModelByUserNameAsync(userToActivate);
            if (dbModifiedUser == null)
            {
                response.ErrorMessage = $"Could not find user '{userToActivate.UserName}'";
            }
            else if (dbModifiedUser.IsActive)
            {
                response.ErrorMessage = $"User '{userToActivate.UserName}' is already activated.";
            }
            else
            {
                dbModifiedUser.IsActive = true;
                dbContext.UserChangeLogs.Add(new UserChangeLog
                {
                    ChangedByUserId = currentUser.UserId,
                    ChangeDesc = $"Activate user; UserId = {dbModifiedUser.UserId}.",
                    ModifiedOn = DateTime.Now,
                    ChangedUserId = currentUser.UserId
                });
                await dbContext.SaveChangesAsync();
                response = new ResponseModel { Success = true };
            }

            return response;
        }

        public async Task<ResponseModel> DeactivateUserAsync(UserModel loggedInUser, UserModel userToDeactivate)
        {
            FiasmErrorHandling.VerifyArgNotNull(loggedInUser);
            FiasmErrorHandling.VerifyArgNotNull(userToDeactivate);

            var currentUser = await GetLoggedInUserFromUserModelAsync(loggedInUser);

            FiasmErrorHandling.VerifyUserPermission(currentUser, ClaimTypes.AuthorizedToDoUserAdministration);

            var response = new ResponseModel { Success = false };

            var dbModifiedUser = await GetDbUserFromUserModelByUserNameAsync(userToDeactivate);
            if (dbModifiedUser == null)
            {
                response.ErrorMessage = $"Could not find user '{userToDeactivate.UserName}'";
            }
            else if(!dbModifiedUser.IsActive)
            {
                response.ErrorMessage = $"User '{userToDeactivate.UserName} is already deactivated.'";
            }
            else
            {
                    dbModifiedUser.IsActive = false;
                    dbContext.UserChangeLogs.Add(new UserChangeLog
                    {
                        ChangedByUserId = currentUser.UserId,
                        ChangeDesc = $"Deactivate user; UserId = {dbModifiedUser.UserId}.",
                        ModifiedOn = DateTime.Now,
                        ChangedUserId = currentUser.UserId
                    });
                    await dbContext.SaveChangesAsync();
                    response = new ResponseModel { Success = true };
            }

            return response;
        }

        public async Task<ResponseModel> DeleteUserAsync(UserModel loggedInUser, UserModel deletedUser)
        {
            FiasmErrorHandling.VerifyArgNotNull(loggedInUser);
            FiasmErrorHandling.VerifyArgNotNull(deletedUser);

            var currentUser = await GetLoggedInUserFromUserModelAsync(loggedInUser);

            FiasmErrorHandling.VerifyUserPermission(currentUser, ClaimTypes.AuthorizedToDoUserAdministration);

            var response = new ResponseModel { Success = false };

            var dbModifiedUser = await GetDbUserFromUserModelByUserNameAsync(deletedUser);
            if (dbModifiedUser == null)
            {
                response.ErrorMessage = $"Could not find user '{deletedUser.UserName}'";
            }
            else
            {
                dbContext.Users.Remove(dbModifiedUser);
                dbContext.UserChangeLogs.Add(new UserChangeLog
                {
                    ChangedByUserId = currentUser.UserId,
                    ChangeDesc = $"Deleted user; UserName = {dbModifiedUser.UserName}.",
                    ModifiedOn = DateTime.Now,
                    ChangedUserId = currentUser.UserId
                });
                await dbContext.SaveChangesAsync();

                response = new ResponseModel { Success = true };
            }

            return response;
        }

        public async Task<ResponseModel> CreateNewUserAsync(UserModel loggedInUser, UserModel newUser, string password)
        {
            FiasmErrorHandling.VerifyArgNotNull(loggedInUser);
            FiasmErrorHandling.VerifyArgNotNull(newUser);

            var currentUser = await GetLoggedInUserFromUserModelAsync(loggedInUser);

            FiasmErrorHandling.VerifyUserPermission(currentUser, ClaimTypes.AuthorizedToDoUserAdministration);

            var response = new ResponseModel { Success = false };

            if ((await GetDbUserFromUserModelByUserNameAsync(newUser)) != null)
            {
                response.ErrorMessage = $"A user '{newUser.UserName}' already exists.";
            }
            else if((await GetDbUserFromUserModelByEmailAsync(newUser)) != null)
            {
                response.ErrorMessage = $"A user with email address '{newUser.EmailAddress}' already exists.";
            }
            else
            {
                dbContext.Users.Add(new User
                {
                    UserName = newUser.UserName,
                    EmailAddress = newUser.EmailAddress,
                    PhoneNumber = newUser.PhoneNumber,
                    HashedPassword = hasher.HashPassword(newUser.UserName, password),
                    IsPasswordReseting = false,
                    IsPasswordResetRequired = true,
                    IsActive = true
                });
                dbContext.UserChangeLogs.Add(new UserChangeLog
                {
                    ChangedByUserId = currentUser.UserId,
                    ChangeDesc = $"Create new user; UserName = {newUser.UserName}.",
                    ModifiedOn = DateTime.Now,
                    ChangedUserId = currentUser.UserId
                });
                await dbContext.SaveChangesAsync();
                response = new ResponseModel { Success = true };
            }

            return response;
        }

        public async Task<IEnumerable<ClaimModel>> GetClaimsAsync(UserModel loggedInUser)
        {
            FiasmErrorHandling.VerifyArgNotNull(loggedInUser);

            var currentUser = await GetLoggedInUserFromUserModelAsync(loggedInUser);

            // only a user admin should be able to see permissions
            FiasmErrorHandling.VerifyUserPermission(currentUser, ClaimTypes.AuthorizedToDoUserAdministration);

            return await dbContext.Claims.Select(c => mapper.Map<ClaimModel>(c)).ToListAsync();
        }

        public async Task<IEnumerable<ClaimModel>> GetUserClaimsAsync(UserModel loggedInUser)
        {
            FiasmErrorHandling.VerifyArgNotNull(loggedInUser);

            var currentUser = await GetLoggedInUserFromUserModelAsync(loggedInUser);

            // only a user admin should be able to see permissions
            FiasmErrorHandling.VerifyUserPermission(currentUser, ClaimTypes.AuthorizedToDoUserAdministration);

            return currentUser.UserClaims.Select(c => mapper.Map<ClaimModel>(c));
        }

        public async Task<ResponseModel> AddClaimToUserAsync(UserModel loggedInUser, UserModel userToModify, ClaimModel claimToAdd)
        {
            FiasmErrorHandling.VerifyArgNotNull(loggedInUser);

            var currentUser = await GetLoggedInUserFromUserModelAsync(loggedInUser);

            var response = new ResponseModel { Success = false };

            var modifiedUser = await GetDbUserFromUserModelByUserNameAsync(userToModify);
            var claim = await dbContext.Claims.FirstOrDefaultAsync(c => c.ClaimType == claimToAdd.ClaimType);

            if (modifiedUser == null)
            {
                response.ErrorMessage = $"Could not find user '{userToModify.UserName}'.";
            }
            else if (claim == null)
            {
                response.ErrorMessage = $"Could not claim type '{claimToAdd.ClaimType}'.";
            }
            else if (modifiedUser.UserClaims.Any(c => c.Claim.ClaimType == claimToAdd.ClaimType))
            {
                response.ErrorMessage = $"User '{userToModify.UserName}' already has claim '{claimToAdd.ClaimType}'.";
            }
            else
            {
                dbContext.UserClaims.Add(new UserClaim
                {
                    User = modifiedUser,
                    Claim = claim
                });
                dbContext.UserChangeLogs.Add(new UserChangeLog
                {
                    ChangedByUserId = currentUser.UserId,
                    ChangeDesc = $"Add claim to user; UserName = {userToModify.UserName}, claimType = '{claimToAdd.ClaimType}'.",
                    ModifiedOn = DateTime.Now,
                    ChangedUserId = currentUser.UserId
                });
                await dbContext.SaveChangesAsync();
                response = new ResponseModel { Success = true };
            }

            return response;
        }

        public async Task<ResponseModel> RemoveClaimFromUserAsync(UserModel loggedInUser, UserModel userToModify, ClaimModel claimToRemove)
        {
            FiasmErrorHandling.VerifyArgNotNull(loggedInUser);

            var currentUser = await GetLoggedInUserFromUserModelAsync(loggedInUser);

            var response = new ResponseModel { Success = false };

            var modifiedUser = await GetDbUserFromUserModelByUserNameAsync(userToModify);
            var claim = await dbContext.Claims.FirstOrDefaultAsync(c => c.ClaimType == claimToRemove.ClaimType);
            var userClaim = modifiedUser.UserClaims.FirstOrDefault(c => c.Claim.ClaimType == claimToRemove.ClaimType);

            if (modifiedUser == null)
            {
                response.ErrorMessage = $"Could not find user '{userToModify.UserName}'.";
            }
            else if (claim == null)
            {
                response.ErrorMessage = $"Could not find claim type '{claimToRemove.ClaimType}'.";
            }
            else if (userClaim == null)
            {
                response.ErrorMessage = $"The user '{modifiedUser.UserName}' does not have claim type '{claimToRemove.ClaimType}'.";
            }
            else
            {
                dbContext.UserClaims.Remove(userClaim);
                dbContext.UserChangeLogs.Add(new UserChangeLog
                {
                    ChangedByUserId = currentUser.UserId,
                    ChangeDesc = $"Remove claim from user; UserName = {userToModify.UserName}, claimType = '{claimToRemove.ClaimType}'.",
                    ModifiedOn = DateTime.Now,
                    ChangedUserId = currentUser.UserId
                });
                await dbContext.SaveChangesAsync();
                response = new ResponseModel { Success = true };
            }

            return response;
        }
    }
}
