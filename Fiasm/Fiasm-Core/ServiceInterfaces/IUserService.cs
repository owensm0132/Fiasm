
using Fiasm.Core.Models.UserModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Fiasm.Core.ServiceInterfaces
{
    public interface IUserService
    {
        #region authentication
        Task<UserModel> AuthenticateUserAsync(LoginModel login);
        #endregion

        #region claims
        Task<bool> DoesUserHaveClaimAsync(UserModel user, string claimType);
        Task<string> GetUserClaimValueAsync(UserModel user, string claimType);
        Task<IEnumerable<ClaimModel>> GetUserClaimsAsync(UserModel user);

        Task AddClaimType(ClaimModel claim);
        Task<bool> SafeDeleteClaim(ClaimModel claim);
        Task ForceDeleteClaim(ClaimModel claim);

        Task AddUserClaim(UserModel user, ClaimModel claim);
        Task RemoveUserClaim(UserModel user, ClaimModel claim);
        Task UpdateUserClaimValue(UserModel user, ClaimModel claim);
        #endregion

        #region accounts
        Task AddUserAsync(UserModel user);
        Task DeleteUserAsync(UserModel user);
        Task DeactivateUserAsync(UserModel user);
        Task ActivateUserAsync(UserModel user);
        #endregion
    }
}
