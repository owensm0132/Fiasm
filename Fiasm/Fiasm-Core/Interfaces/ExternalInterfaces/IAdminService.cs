using Fiasm.Core.Models.RequestModels;
using Fiasm.Core.Models.UserModels;
using System;
using System.Collections.Generic;
using System.Security.Principal;
using System.Text;

namespace Fiasm.Core.Interfaces.ExternalInterfaces
{
    public interface IAdminService
    {
        UserModel AuthenticateUser(LoginModel userLogin);

        IEnumerable<UserModel> GetUsers(UserModel currentUser);

        ResponseModel UpdateUser(UserModel currentUser, UserModel modifiedUser);

        ResponseModel DeleteUser(UserModel currentUser, UserModel deletedUser);

        ResponseModel CreateNewUser(UserModel currentUser, UserModel newUser);
    }
}
