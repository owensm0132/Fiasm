using Fiasm.Core.Models.RequestModels;
using Fiasm.Core.Models.UserModels;
using System;
using System.Collections.Generic;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;

namespace Fiasm.Core.Interfaces.ExternalInterfaces
{
    public interface IAdminService
    {
        Task<UserModel> AuthenticateUserAsync(LoginModel userLogin);

        Task<IEnumerable<UserModel>> GetUsersAsync(UserModel loggedInUser);

        /// <summary>
        /// Change the user email or phone number
        /// </summary>
        /// <param name="loggedInUser"></param>
        /// <param name="modifiedUser"></param>
        /// <returns></returns>
        Task<ResponseModel> UpdateUserAsync(UserModel loggedInUser, UserModel modifiedUser);

        Task<ResponseModel> ActivateUserAsync(UserModel loggedInUser, UserModel userToActivate);

        Task<ResponseModel> DeactivateUserAsync(UserModel loggedInUser, UserModel userToDeactivate);

        Task<ResponseModel> DeleteUserAsync(UserModel loggedInUser, UserModel deletedUser);

        Task<ResponseModel> CreateNewUserAsync(UserModel loggedInUser, UserModel newUser, string password);

        Task<IEnumerable<ClaimModel>> GetClaimsAsync(UserModel loggedInUser);

        Task<IEnumerable<ClaimModel>> GetUserClaimsAsync(UserModel loggedInUser);

        Task<ResponseModel> AddClaimToUserAsync(UserModel loggedInUser, UserModel userToModify, ClaimModel claimToAdd);

        Task<ResponseModel> RemoveClaimFromUserAsync(UserModel loggedInUser, UserModel userToModify, ClaimModel claimToRemove);
    }
}
