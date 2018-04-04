using Xunit;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using Fiasm.Data;
using Fiasm.Data.EntityModels;
using System.Linq;
using System;
using Fiasm.Data.Utilities;
using Fiasm.Core.Services.ExternalServices;
using Fiasm.Core.Models.UserModels;
using AutoMapper;
using System.Security.Authentication;

namespace Fiasm.UnitTests
{
    public class AdminServiceTests
    {
        #region private variables
        IMapper mapper = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<User, UserModel>();
        }).CreateMapper();

        #endregion

        #region private helper functions

        private Claim AddClaim(FiasmDbContext dbContext, string claimType,
            string claimValue)
        {
            Claim claim = null;
            if (!dbContext.Claims.Any(c => c.ClaimType == claimType))
            {
                claim = new Claim
                {
                    ClaimType = claimType,
                    ClaimValue = claimValue
                };
                dbContext.Claims.Add(claim);
                dbContext.SaveChanges();
            }
            return claim;
        }

        private User AddUser(FiasmDbContext dbContext, string userName,
            string userEmail, string password, IHasher hasher)
        {
            User user = null;
            if (!dbContext.Users.Any(c => c.UserName == userName || c.EmailAddress == userEmail))
            {
                user = new User
                {
                    UserName = userName,
                    EmailAddress = userEmail,
                    HashedPassword = hasher.HashPassword(userName, password),
                    IsActive = true
                };
                dbContext.Users.Add(user);
                dbContext.SaveChanges();
            }
            return user;
        }

        private void AddUserClaims(FiasmDbContext dbContext, string userName,
            List<string> userClaimTypes)
        {
            // get user
            var user = dbContext.Users.FirstOrDefault(c => c.UserName == userName);
            if (user == null)
            {
                throw new Exception($"No user exists in database for {userName}.");
            }
            // get claims
            var claims = dbContext.Claims.Where(c => userClaimTypes.Contains(c.ClaimType)).ToList();
            if (claims.Count != userClaimTypes.Count)
            {
                throw new Exception($"Could not find all user claims for {string.Join(",", userClaimTypes)}.");
            }

            foreach (var claim in claims)
            {
                dbContext.UserClaims.Add(new UserClaim
                {
                    User = user,
                    Claim = claim
                });
                dbContext.SaveChanges();
            }
        }
        #endregion

        #region Test AuthenticateUserAsync

        [Fact]
        public void CanAuthenticateBasicUser()
        {
            var dbOptions = new DbContextOptionsBuilder<FiasmDbContext>()
                .UseInMemoryDatabase(databaseName: "CanAuthenticateBasicUser").Options;
            IHasher hasher = new Hasher();

            string userName = "testUser";
            string userEmail = "test@test.com";
            string password = "P@ssword1";
            string authUserClaim = Fiasm.Core.Models.UserModels.ClaimTypes.AuthorizedUser.ToString();

            using (var dbContext = new FiasmDbContext(dbOptions))
            {
                // create the basic user
                var basicUser = AddUser(dbContext, userName, userEmail, password, hasher);
                var claimAuthUser = AddClaim(dbContext, authUserClaim, "true");

                Assert.NotNull(basicUser);
                Assert.NotNull(claimAuthUser);

                AddUserClaims(dbContext, userName, new List<string> { authUserClaim });

                // try to authenticate basic user
                AdminService adminService = new AdminService(dbContext, hasher);
                var testUser = adminService.AuthenticateUserAsync(new Core.Models.UserModels.LoginModel
                {
                    LoginName = basicUser.UserName,
                    PassWord = password
                }).Result;
                Assert.NotNull(testUser);
            }
        }

        [Fact]
        public void CanAuthenticateUserAdminUser()
        {
            var dbOptions = new DbContextOptionsBuilder<FiasmDbContext>()
                .UseInMemoryDatabase(databaseName: "CanAuthenticateUserAdminUser").Options;
            IHasher hasher = new Hasher();

            string userName = "testUser";
            string userEmail = "test@test.com";
            string password = "P@ssword1";
            string adminUserClaim = Core.Models.UserModels.ClaimTypes.AuthorizedToDoUserAdministration.ToString();
            string authUserClaim = Core.Models.UserModels.ClaimTypes.AuthorizedUser.ToString();

            using (var dbContext = new FiasmDbContext(dbOptions))
            {
                // create the admin user
                var userAdminUser = AddUser(dbContext, userName, userEmail, password, hasher);
                var claimAdminClaim = AddClaim(dbContext, adminUserClaim, "true");
                var claimAuthUser = AddClaim(dbContext, authUserClaim, "true");

                Assert.NotNull(userAdminUser);
                Assert.NotNull(claimAdminClaim);
                Assert.NotNull(claimAuthUser);

                AddUserClaims(dbContext, userName, new List<string> { adminUserClaim, authUserClaim });

                // try to authenticate basic user
                AdminService adminService = new AdminService(dbContext, hasher);
                Assert.NotNull(adminService);
                var testUser = adminService.AuthenticateUserAsync(new Core.Models.UserModels.LoginModel
                {
                    LoginName = userAdminUser.UserName,
                    PassWord = password
                }).Result;
                Assert.NotNull(testUser);
            }
        }

        [Fact]
        public void CannotAuthenticateInvalidUser()
        {
            var dbOptions = new DbContextOptionsBuilder<FiasmDbContext>()
                .UseInMemoryDatabase(databaseName: "CannotAuthenticateInvalidUser").Options;
            IHasher hasher = new Hasher();

            string userName = "testUser";
            string password = "P@ssword1";

            using (var dbContext = new FiasmDbContext(dbOptions))
            {
                // try to authenticate basic user
                AdminService adminService = new AdminService(dbContext, hasher);
                var testUser = adminService.AuthenticateUserAsync(new Core.Models.UserModels.LoginModel
                {
                    LoginName = userName,
                    PassWord = password
                }).Result;
                Assert.Null(testUser);
            }
        }

        [Fact]
        public void CannotAuthenticateUserWithMissingAuthUserClaim()
        {
            var dbOptions = new DbContextOptionsBuilder<FiasmDbContext>()
                .UseInMemoryDatabase(databaseName: "CannotAuthenticateUserWithMissingAuthUserClaim").Options;
            IHasher hasher = new Hasher();

            string userName = "testUser";
            string userEmail = "test@test.com";
            string password = "P@ssword1";
            string adminUserClaim = Core.Models.UserModels.ClaimTypes.AuthorizedToDoUserAdministration.ToString();

            using (var dbContext = new FiasmDbContext(dbOptions))
            {
                // create the admin user with missingauth claim
                var userAdminUser = AddUser(dbContext, userName, userEmail, password, hasher);
                var claimAdminClaim = AddClaim(dbContext, adminUserClaim, "true");

                Assert.NotNull(userAdminUser);
                Assert.NotNull(claimAdminClaim);

                AddUserClaims(dbContext, userName, new List<string> { adminUserClaim });

                // try to authenticate basic user
                AdminService adminService = new AdminService(dbContext, hasher);
                Assert.NotNull(adminService);
                var testUser = adminService.AuthenticateUserAsync(new Core.Models.UserModels.LoginModel
                {
                    LoginName = userAdminUser.UserName,
                    PassWord = password
                }).Result;
                Assert.Null(testUser);
            }
        }

        [Fact]
        public void CannotAuthenticateInActiveUser()
        {
            var dbOptions = new DbContextOptionsBuilder<FiasmDbContext>()
                .UseInMemoryDatabase(databaseName: "CannotAuthenticateInActiveUser").Options;
            IHasher hasher = new Hasher();

            string userName = "testUser";
            string userEmail = "test@test.com";
            string password = "P@ssword1";
            string adminUserClaim = Core.Models.UserModels.ClaimTypes.AuthorizedToDoUserAdministration.ToString();

            using (var dbContext = new FiasmDbContext(dbOptions))
            {
                // create the admin user with missingauth claim
                var userAdminUser = AddUser(dbContext, userName, userEmail, password, hasher);
                var claimAdminClaim = AddClaim(dbContext, adminUserClaim, "true");

                Assert.NotNull(userAdminUser);
                Assert.NotNull(claimAdminClaim);

                AddUserClaims(dbContext, userName, new List<string> { adminUserClaim });

                // deactivate user
                userAdminUser.IsActive = false;
                dbContext.SaveChanges();

                // try to authenticate basic user
                AdminService adminService = new AdminService(dbContext, hasher);
                Assert.NotNull(adminService);
                var testUser = adminService.AuthenticateUserAsync(new Core.Models.UserModels.LoginModel
                {
                    LoginName = userAdminUser.UserName,
                    PassWord = password
                }).Result;
                Assert.Null(testUser);
            }
        }
        #endregion

        #region Test CanGetAllUsers
        [Fact]
        public void CanGetAllUsers()
        {
            var dbOptions = new DbContextOptionsBuilder<FiasmDbContext>()
                .UseInMemoryDatabase(databaseName: "CanGetAllUsers").Options;
            IHasher hasher = new Hasher();

            string userName = "testUser";
            string password = "P@ssword1";
            string authUserClaim = Core.Models.UserModels.ClaimTypes.AuthorizedUser.ToString();
            int numberOfUsers = 10;

            using (var dbContext = new FiasmDbContext(dbOptions))
            {
                var users = new List<User>();
                var claimAuthUser = AddClaim(dbContext, authUserClaim, "true");

                // create logged in user
                users.Add(AddUser(dbContext, userName, $"test@test.com", password, hasher));
                Assert.NotNull(users.Last());
                AddUserClaims(dbContext, userName, new List<string> { authUserClaim });

                // create other users
                for (int i = 0; i < numberOfUsers; i++)
                {
                    users.Add(AddUser(dbContext, userName + i, $"test{i}@test.com", password, hasher));
                    Assert.NotNull(users.Last());
                    AddUserClaims(dbContext, userName + i, new List<string> { authUserClaim });
                }

                AdminService adminService = new AdminService(dbContext, hasher);
                var loggedInUser = adminService.AuthenticateUserAsync(new Core.Models.UserModels.LoginModel
                {
                    LoginName = userName,
                    PassWord = password
                }).Result;
                Assert.NotNull(loggedInUser);

                var testUsers = adminService.GetUsersAsync(loggedInUser).Result.ToList();
                Assert.NotNull(testUsers);
                Assert.True(testUsers.Count == numberOfUsers + 1);
            }
        }
        #endregion

        #region Test UpdateUserAsync
        [Fact]
        public void UserCannotUpdateOtherUserWithoutUserAdminClaim()
        {
            var dbOptions = new DbContextOptionsBuilder<FiasmDbContext>()
                .UseInMemoryDatabase(databaseName: "UserCannotUpdateOtherUserWithoutUserAdminClaim").Options;
            IHasher hasher = new Hasher();

            string userName = "testUser";
            string password = "P@ssword1";
            string authUserClaim = Core.Models.UserModels.ClaimTypes.AuthorizedUser.ToString();

            using (var dbContext = new FiasmDbContext(dbOptions))
            {
                var claimAuthUser = AddClaim(dbContext, authUserClaim, "true");

                // create logged in user
                var user = AddUser(dbContext, userName, $"test@test.com", password, hasher);
                Assert.NotNull(user);
                AddUserClaims(dbContext, userName, new List<string> { authUserClaim });

                // create other user
                var otherUser = AddUser(dbContext, userName + "other", $"testOther@test.com", password, hasher);
                Assert.NotNull(otherUser);
                AddUserClaims(dbContext, userName + "other", new List<string> { authUserClaim });

                AdminService adminService = new AdminService(dbContext, hasher);
                var loggedInUser = adminService.AuthenticateUserAsync(new LoginModel
                {
                    LoginName = userName,
                    PassWord = password
                }).Result;
                Assert.NotNull(loggedInUser);

                // get UserModel of other user and make a modification
                UserModel otherUserModel = mapper.Map<UserModel>(otherUser);
                Assert.NotNull(otherUserModel);
                otherUserModel.EmailAddress = "abc@123.com";

                // try to change a other user
                var didThrow = false;
                try
                {
                    var response = adminService.UpdateUserAsync(loggedInUser, otherUserModel).Result;
                    Assert.True(response.Success);
                }
                catch
                {
                    didThrow = true;
                }

                Assert.True(didThrow);
            }
        }

        [Fact]
        public void UserCanUpdateOtherUserInfoWithUserAdminClaim()
        {
            var dbOptions = new DbContextOptionsBuilder<FiasmDbContext>()
                .UseInMemoryDatabase(databaseName: "UserCanUpdateOtherUserInfoWithUserAdminClaim").Options;
            IHasher hasher = new Hasher();

            string userName = "testUser";
            string otherUserName = userName + "other";
            string password = "P@ssword1";
            string newUserEmail = "abc@123.com";
            string newUserPhone = "1232343456";
            string authUserClaim = ClaimTypes.AuthorizedUser.ToString();
            string authUserAdminClaim = ClaimTypes.AuthorizedToDoUserAdministration.ToString();

            using (var dbContext = new FiasmDbContext(dbOptions))
            {
                var claimAuthUser = AddClaim(dbContext, authUserClaim, "true");
                var claimAuthAdminUser = AddClaim(dbContext, authUserAdminClaim, "true");

                // create logged in user
                var user = AddUser(dbContext, userName, $"test@test.com", password, hasher);
                Assert.NotNull(user);
                AddUserClaims(dbContext, userName, new List<string> { authUserClaim, authUserAdminClaim });

                // create other user
                var otherUser = AddUser(dbContext, otherUserName, $"testOther@test.com", password, hasher);
                Assert.NotNull(otherUser);
                AddUserClaims(dbContext, userName + "other", new List<string> { authUserClaim });

                AdminService adminService = new AdminService(dbContext, hasher);
                var loggedInUser = adminService.AuthenticateUserAsync(new LoginModel
                {
                    LoginName = userName,
                    PassWord = password
                }).Result;
                Assert.NotNull(loggedInUser);

                // get UserModel of other user and make a modification
                UserModel otherUserModel = mapper.Map<UserModel>(otherUser);
                Assert.NotNull(otherUserModel);
                otherUserModel.EmailAddress = newUserEmail;
                otherUserModel.PhoneNumber = newUserPhone;

                // try to change a other user
                var didThrow = false;
                try
                {
                    var response = adminService.UpdateUserAsync(loggedInUser, otherUserModel).Result;
                    Assert.True(response.Success);
                }
                catch
                {
                    didThrow = true;
                }

                Assert.False(didThrow);
                var testOtherUser = dbContext.Users.FirstOrDefault(u => u.UserName == otherUserName);
                Assert.NotNull(testOtherUser);
                var testOtherUserModel = mapper.Map<UserModel>(testOtherUser);
                Assert.NotNull(otherUserModel);
                Assert.Same(newUserEmail, testOtherUserModel.EmailAddress);
                Assert.Same(newUserPhone, testOtherUserModel.PhoneNumber);
            }
        }

        [Fact]
        public void UserCanUpdateOwnUserInfoWithoutUserAdminClaim()
        {
            var dbOptions = new DbContextOptionsBuilder<FiasmDbContext>()
                .UseInMemoryDatabase(databaseName: "UserCanUpdateOwnUserInfoWithoutUserAdminClaim").Options;
            IHasher hasher = new Hasher();

            string userName = "testUser";
            string password = "P@ssword1";
            string newUserEmail = "abc@123.com";
            string newUserPhone = "1232343456";
            string authUserClaim = ClaimTypes.AuthorizedUser.ToString();

            using (var dbContext = new FiasmDbContext(dbOptions))
            {
                var claimAuthUser = AddClaim(dbContext, authUserClaim, "true");

                // create logged in user
                var user = AddUser(dbContext, userName, $"test@test.com", password, hasher);
                Assert.NotNull(user);
                AddUserClaims(dbContext, userName, new List<string> { authUserClaim });

                AdminService adminService = new AdminService(dbContext, hasher);
                var loggedInUser = adminService.AuthenticateUserAsync(new LoginModel
                {
                    LoginName = userName,
                    PassWord = password
                }).Result;
                Assert.NotNull(loggedInUser);

                // get UserModel of other user and make a modification
                loggedInUser.EmailAddress = newUserEmail;
                loggedInUser.PhoneNumber = newUserPhone;

                // try to change a other user
                var didThrow = false;
                try
                {
                    var response = adminService.UpdateUserAsync(loggedInUser, loggedInUser).Result;
                    Assert.True(response.Success);
                }
                catch
                {
                    didThrow = true;
                }

                Assert.False(didThrow);

                Assert.Same(newUserEmail, loggedInUser.EmailAddress);
                Assert.Same(newUserPhone, loggedInUser.PhoneNumber);
            }
        }
        #endregion

        #region Test ActivateUserAsync
        [Fact]
        public void UserCannotActivateAnoyoneWithoutUserAdminClaim()
        {
            var dbOptions = new DbContextOptionsBuilder<FiasmDbContext>()
                .UseInMemoryDatabase(databaseName: "UserCannotActivateAnoyoneWithoutUserAdminClaim").Options;
            IHasher hasher = new Hasher();

            string userName = "testUser";
            string password = "P@ssword1";
            string authUserClaim = Core.Models.UserModels.ClaimTypes.AuthorizedUser.ToString();

            using (var dbContext = new FiasmDbContext(dbOptions))
            {
                var claimAuthUser = AddClaim(dbContext, authUserClaim, "true");

                // create logged in user
                var user = AddUser(dbContext, userName, $"test@test.com", password, hasher);
                Assert.NotNull(user);
                AddUserClaims(dbContext, userName, new List<string> { authUserClaim });

                // create other user
                var otherUser = AddUser(dbContext, userName + "other", $"testOther@test.com", password, hasher);
                Assert.NotNull(otherUser);
                AddUserClaims(dbContext, userName + "other", new List<string> { authUserClaim });

                // deactivate the user
                otherUser.IsActive = false;
                dbContext.SaveChanges();

                AdminService adminService = new AdminService(dbContext, hasher);
                var loggedInUser = adminService.AuthenticateUserAsync(new LoginModel
                {
                    LoginName = userName,
                    PassWord = password
                }).Result;
                Assert.NotNull(loggedInUser);

                // get UserModel of other user and make a modification
                UserModel otherUserModel = mapper.Map<UserModel>(otherUser);
                Assert.NotNull(otherUserModel);

                // try to activate the other user
                var didThrow = false;
                try
                {
                    var response = adminService.ActivateUserAsync(loggedInUser, otherUserModel).Result;
                    Assert.True(response.Success);
                }
                catch
                {
                    didThrow = true;
                }

                Assert.True(didThrow);
            }
        }

        [Fact]
        public void UserCanActivateAnoyoneWithUserAdminClaim()
        {
            var dbOptions = new DbContextOptionsBuilder<FiasmDbContext>()
                .UseInMemoryDatabase(databaseName: "UserCanActivateAnoyoneWithUserAdminClaim").Options;
            IHasher hasher = new Hasher();

            string userName = "testUser";
            string password = "P@ssword1";
            string otherUserName = userName + "other";
            string authUserClaim = Core.Models.UserModels.ClaimTypes.AuthorizedUser.ToString();
            string authAdminUserClaim = Core.Models.UserModels.ClaimTypes.AuthorizedToDoUserAdministration.ToString();

            using (var dbContext = new FiasmDbContext(dbOptions))
            {
                var claimAuthUser = AddClaim(dbContext, authUserClaim, "true");
                var claimAuthUserAdmin = AddClaim(dbContext, authAdminUserClaim, "true");

                // create logged in user
                var user = AddUser(dbContext, userName, $"test@test.com", password, hasher);
                Assert.NotNull(user);
                AddUserClaims(dbContext, userName, new List<string> { authUserClaim, authAdminUserClaim });

                // create other user
                var otherUser = AddUser(dbContext, otherUserName, $"testOther@test.com", password, hasher);
                Assert.NotNull(otherUser);
                AddUserClaims(dbContext, userName + "other", new List<string> { authUserClaim });

                // deactivate the user
                otherUser.IsActive = false;
                dbContext.SaveChanges();
                Assert.False(dbContext.Users.FirstOrDefault(u => u.UserName == otherUserName)?.IsActive ?? true);

                AdminService adminService = new AdminService(dbContext, hasher);
                var loggedInUser = adminService.AuthenticateUserAsync(new LoginModel
                {
                    LoginName = userName,
                    PassWord = password
                }).Result;
                Assert.NotNull(loggedInUser);

                // get UserModel of other user and make a modification
                UserModel otherUserModel = mapper.Map<UserModel>(otherUser);
                Assert.NotNull(otherUserModel);

                // try to activate the other user
                var didThrow = false;
                try
                {
                    var response = adminService.ActivateUserAsync(loggedInUser, otherUserModel).Result;
                    Assert.True(response.Success);
                }
                catch
                {
                    didThrow = true;
                }

                Assert.False(didThrow);
                Assert.True(dbContext.Users.FirstOrDefault(u => u.UserName == otherUserName)?.IsActive ?? false);
            }
        }
        #endregion

        #region Test DeactivateUser
        [Fact]
        public void UserCannotDeactivateAnoyoneWithoutUserAdminClaim()
        {
            var dbOptions = new DbContextOptionsBuilder<FiasmDbContext>()
                .UseInMemoryDatabase(databaseName: "UserCannotDeactivateAnoyoneWithoutUserAdminClaim").Options;
            IHasher hasher = new Hasher();

            string userName = "testUser";
            string password = "P@ssword1";
            string authUserClaim = Core.Models.UserModels.ClaimTypes.AuthorizedUser.ToString();

            using (var dbContext = new FiasmDbContext(dbOptions))
            {
                var claimAuthUser = AddClaim(dbContext, authUserClaim, "true");

                // create logged in user
                var user = AddUser(dbContext, userName, $"test@test.com", password, hasher);
                Assert.NotNull(user);
                AddUserClaims(dbContext, userName, new List<string> { authUserClaim });

                // create other user
                var otherUser = AddUser(dbContext, userName + "other", $"testOther@test.com", password, hasher);
                Assert.NotNull(otherUser);
                AddUserClaims(dbContext, userName + "other", new List<string> { authUserClaim });

                AdminService adminService = new AdminService(dbContext, hasher);
                var loggedInUser = adminService.AuthenticateUserAsync(new LoginModel
                {
                    LoginName = userName,
                    PassWord = password
                }).Result;
                Assert.NotNull(loggedInUser);

                // get UserModel of other user and make a modification
                UserModel otherUserModel = mapper.Map<UserModel>(otherUser);
                Assert.NotNull(otherUserModel);

                // try to deactivate the other user
                var didThrow = false;
                try
                {
                    var response = adminService.DeactivateUserAsync(loggedInUser, otherUserModel).Result;
                    Assert.True(response.Success);
                }
                catch
                {
                    didThrow = true;
                }

                Assert.True(didThrow);
            }
        }

        [Fact]
        public void UserCanDeactivateAAnotherUserWithUserAdminClaim()
        {
            var dbOptions = new DbContextOptionsBuilder<FiasmDbContext>()
                .UseInMemoryDatabase(databaseName: "UserCanDeactivateAAnotherUserWithUserAdminClaim").Options;
            IHasher hasher = new Hasher();

            string userName = "testUser";
            string password = "P@ssword1";
            string otherUserName = userName + "other";
            string authUserClaim = Core.Models.UserModels.ClaimTypes.AuthorizedUser.ToString();
            string authAdminUserClaim = Core.Models.UserModels.ClaimTypes.AuthorizedToDoUserAdministration.ToString();

            using (var dbContext = new FiasmDbContext(dbOptions))
            {
                var claimAuthUser = AddClaim(dbContext, authUserClaim, "true");
                var claimAuthUserAdmin = AddClaim(dbContext, authAdminUserClaim, "true");

                // create logged in user
                var user = AddUser(dbContext, userName, $"test@test.com", password, hasher);
                Assert.NotNull(user);
                AddUserClaims(dbContext, userName, new List<string> { authUserClaim, authAdminUserClaim });

                // create other user
                var otherUser = AddUser(dbContext, otherUserName, $"testOther@test.com", password, hasher);
                Assert.NotNull(otherUser);
                AddUserClaims(dbContext, userName + "other", new List<string> { authUserClaim });

                AdminService adminService = new AdminService(dbContext, hasher);
                var loggedInUser = adminService.AuthenticateUserAsync(new LoginModel
                {
                    LoginName = userName,
                    PassWord = password
                }).Result;
                Assert.NotNull(loggedInUser);

                // get UserModel of other user
                UserModel otherUserModel = mapper.Map<UserModel>(otherUser);
                Assert.NotNull(otherUserModel);

                // try to deactivate the other user
                var didThrow = false;
                try
                {
                    var response = adminService.DeactivateUserAsync(loggedInUser, otherUserModel).Result;
                    Assert.True(response.Success);
                }
                catch
                {
                    didThrow = true;
                }

                Assert.False(didThrow);
                var dbUser = dbContext.Users.FirstOrDefault(u => u.UserName == otherUserName);
                Assert.NotNull(dbUser);
                Assert.False(dbUser.IsActive);
            }
        }

        [Fact]
        public void UserCannotDeactivateSelf()
        {
            var dbOptions = new DbContextOptionsBuilder<FiasmDbContext>()
                .UseInMemoryDatabase(databaseName: "UserCannotDeactivateSelf").Options;
            IHasher hasher = new Hasher();

            string userName = "testUser";
            string password = "P@ssword1";
            string authUserClaim = Core.Models.UserModels.ClaimTypes.AuthorizedUser.ToString();
            string authAdminUserClaim = Core.Models.UserModels.ClaimTypes.AuthorizedToDoUserAdministration.ToString();

            using (var dbContext = new FiasmDbContext(dbOptions))
            {
                var claimAuthUser = AddClaim(dbContext, authUserClaim, "true");
                var claimAuthUserAdmin = AddClaim(dbContext, authAdminUserClaim, "true");

                // create logged in user
                var user = AddUser(dbContext, userName, $"test@test.com", password, hasher);
                Assert.NotNull(user);
                AddUserClaims(dbContext, userName, new List<string> { authUserClaim, authAdminUserClaim });

                AdminService adminService = new AdminService(dbContext, hasher);
                var loggedInUser = adminService.AuthenticateUserAsync(new LoginModel
                {
                    LoginName = userName,
                    PassWord = password
                }).Result;
                Assert.NotNull(loggedInUser);

                // try to activate the other user
                var didThrow = false;
                try
                {
                    var response = adminService.DeactivateUserAsync(loggedInUser, loggedInUser).Result;
                    Assert.True(response.Success);
                }
                catch
                {
                    didThrow = true;
                }

                Assert.True(didThrow);
                var dbUser = dbContext.Users.FirstOrDefault(u => u.UserName == loggedInUser.UserName);
                Assert.NotNull(dbUser);
                Assert.True(dbUser.IsActive);
            }
        }
        #endregion

        #region test DeleteUserAsync
        [Fact]
        public void UserCannotDeleteAnoyoneWithoutUserAdminClaim()
        {
            var dbOptions = new DbContextOptionsBuilder<FiasmDbContext>()
                .UseInMemoryDatabase(databaseName: "UserCannotDeleteAnoyoneWithoutUserAdminClaim").Options;
            IHasher hasher = new Hasher();

            string userName = "testUser";
            string otherUserName = "testUserOther";
            string password = "P@ssword1";
            string authUserClaim = Core.Models.UserModels.ClaimTypes.AuthorizedUser.ToString();

            using (var dbContext = new FiasmDbContext(dbOptions))
            {
                var claimAuthUser = AddClaim(dbContext, authUserClaim, "true");

                // create logged in user
                var user = AddUser(dbContext, userName, $"test@test.com", password, hasher);
                Assert.NotNull(user);
                AddUserClaims(dbContext, userName, new List<string> { authUserClaim });

                // create other user
                var otherUser = AddUser(dbContext, otherUserName, $"testOther@test.com", password, hasher);
                Assert.NotNull(otherUser);
                AddUserClaims(dbContext, otherUserName, new List<string> { authUserClaim });

                AdminService adminService = new AdminService(dbContext, hasher);
                var loggedInUser = adminService.AuthenticateUserAsync(new LoginModel
                {
                    LoginName = userName,
                    PassWord = password
                }).Result;
                Assert.NotNull(loggedInUser);

                // get UserModel of other user
                UserModel otherUserModel = mapper.Map<UserModel>(otherUser);
                Assert.NotNull(otherUserModel);

                // try to delete the other user
                var didThrow = false;
                try
                {
                    var response = adminService.DeleteUserAsync(loggedInUser, otherUserModel).Result;
                    Assert.True(response.Success);
                }
                catch
                {
                    didThrow = true;
                }

                Assert.True(didThrow);
            }
        }

        [Fact]
        public void UserCanDeleteOtherUsersWithUserAdminClaim()
        {
            var dbOptions = new DbContextOptionsBuilder<FiasmDbContext>()
                .UseInMemoryDatabase(databaseName: "UserCanDeleteOtherUsersWithUserAdminClaim").Options;
            IHasher hasher = new Hasher();

            string userName = "testUser";
            string otherUserName = "testUserOther";
            string password = "P@ssword1";
            string authUserClaim = Core.Models.UserModels.ClaimTypes.AuthorizedUser.ToString();
            string authAdminUserClaim = Core.Models.UserModels.ClaimTypes.AuthorizedToDoUserAdministration.ToString();

            using (var dbContext = new FiasmDbContext(dbOptions))
            {
                var claimAuthUser = AddClaim(dbContext, authUserClaim, "true");
                var claimAuthAdminUser = AddClaim(dbContext, authAdminUserClaim, "true");

                // create logged in user
                var user = AddUser(dbContext, userName, $"test@test.com", password, hasher);
                Assert.NotNull(user);
                AddUserClaims(dbContext, userName, new List<string> { authUserClaim, authAdminUserClaim });

                // create other user
                var otherUser = AddUser(dbContext, otherUserName, $"testOther@test.com", password, hasher);
                Assert.NotNull(otherUser);
                AddUserClaims(dbContext, otherUserName, new List<string> { authUserClaim });

                AdminService adminService = new AdminService(dbContext, hasher);
                var loggedInUser = adminService.AuthenticateUserAsync(new LoginModel
                {
                    LoginName = userName,
                    PassWord = password
                }).Result;
                Assert.NotNull(loggedInUser);

                // get UserModel of other user 
                UserModel otherUserModel = mapper.Map<UserModel>(otherUser);
                Assert.NotNull(otherUserModel);
                Assert.True(dbContext.Users.Any(u => u.UserName == otherUserName));

                // try to delete the other user
                var didThrow = false;
                try
                {
                    var response = adminService.DeleteUserAsync(loggedInUser, otherUserModel).Result;
                    Assert.True(response.Success);
                }
                catch
                {
                    didThrow = true;
                }

                Assert.False(didThrow);
                Assert.False(dbContext.Users.Any(u => u.UserName == otherUserName));
            }
        }

        [Fact]
        public void UserCannotDeleteSelf()
        {
            var dbOptions = new DbContextOptionsBuilder<FiasmDbContext>()
                .UseInMemoryDatabase(databaseName: "UserCannotDeleteSelf").Options;
            IHasher hasher = new Hasher();

            string userName = "testUser";
            string password = "P@ssword1";
            string authUserClaim = Core.Models.UserModels.ClaimTypes.AuthorizedUser.ToString();
            string authAdminUserClaim = Core.Models.UserModels.ClaimTypes.AuthorizedToDoUserAdministration.ToString();

            using (var dbContext = new FiasmDbContext(dbOptions))
            {
                var claimAuthUser = AddClaim(dbContext, authUserClaim, "true");
                var claimAuthAdminUser = AddClaim(dbContext, authAdminUserClaim, "true");

                // create logged in user
                var user = AddUser(dbContext, userName, $"test@test.com", password, hasher);
                Assert.NotNull(user);
                AddUserClaims(dbContext, userName, new List<string> { authUserClaim, authAdminUserClaim });

                AdminService adminService = new AdminService(dbContext, hasher);
                var loggedInUser = adminService.AuthenticateUserAsync(new LoginModel
                {
                    LoginName = userName,
                    PassWord = password
                }).Result;
                Assert.NotNull(loggedInUser);

                // try to delete the other user
                var didThrow = false;
                try
                {
                    var response = adminService.DeleteUserAsync(loggedInUser, loggedInUser).Result;
                    Assert.True(response.Success);
                }
                catch
                {
                    didThrow = true;
                }

                Assert.True(didThrow);
                Assert.True(dbContext.Users.Any(u => u.UserName == loggedInUser.UserName));
            }
        }
        #endregion

        #region Test CreateNewUserAsync
        [Fact]
        public void UserCannotCreateAnotherUserWithoutUserAdminClaim()
        {
            var dbOptions = new DbContextOptionsBuilder<FiasmDbContext>()
                .UseInMemoryDatabase(databaseName: "UserCannotCreateAnotherUserWithoutUserAdminClaim").Options;
            IHasher hasher = new Hasher();

            string userName = "testUser";
            string otherUserName = "testUserOther";
            string password = "P@ssword1";
            string authUserClaim = Core.Models.UserModels.ClaimTypes.AuthorizedUser.ToString();

            using (var dbContext = new FiasmDbContext(dbOptions))
            {
                var claimAuthUser = AddClaim(dbContext, authUserClaim, "true");

                // create logged in user
                var user = AddUser(dbContext, userName, $"test@test.com", password, hasher);
                Assert.NotNull(user);
                AddUserClaims(dbContext, userName, new List<string> { authUserClaim });

                AdminService adminService = new AdminService(dbContext, hasher);
                var loggedInUser = adminService.AuthenticateUserAsync(new LoginModel
                {
                    LoginName = userName,
                    PassWord = password
                }).Result;
                Assert.NotNull(loggedInUser);

                // try to create another user
                var didThrow = false;
                try
                {
                    var response = adminService.CreateNewUserAsync(loggedInUser,
                        new UserModel
                        {
                            UserName = otherUserName,
                            EmailAddress = $"{otherUserName}@test.com"
                        },
                        password).Result;
                    Assert.True(response.Success);
                }
                catch
                {
                    didThrow = true;
                }

                Assert.True(didThrow);
            }
        }

        [Fact]
        public void UserCanCreateAnotherUserWithUserAdminClaim()
        {
            var dbOptions = new DbContextOptionsBuilder<FiasmDbContext>()
                .UseInMemoryDatabase(databaseName: "UserCanCreateAnotherUserWithUserAdminClaim").Options;
            IHasher hasher = new Hasher();

            string userName = "testUser";
            string otherUserName = "testUserOther";
            string password = "P@ssword1";
            string otherUsersPassword = "P@ssword2";
            string authUserClaim = Core.Models.UserModels.ClaimTypes.AuthorizedUser.ToString();
            string authAdminUserClaim = Core.Models.UserModels.ClaimTypes.AuthorizedToDoUserAdministration.ToString();

            using (var dbContext = new FiasmDbContext(dbOptions))
            {
                var claimAuthUser = AddClaim(dbContext, authUserClaim, "true");
                var claimAuthAdminUser = AddClaim(dbContext, authAdminUserClaim, "true");

                // create logged in user
                var user = AddUser(dbContext, userName, $"test@test.com", password, hasher);
                Assert.NotNull(user);
                AddUserClaims(dbContext, userName, new List<string> { authUserClaim, authAdminUserClaim });

                AdminService adminService = new AdminService(dbContext, hasher);
                var loggedInUser = adminService.AuthenticateUserAsync(new LoginModel
                {
                    LoginName = userName,
                    PassWord = password
                }).Result;
                Assert.NotNull(loggedInUser);

                // try to delete the other user
                var didThrow = false;
                try
                {
                    var response = adminService.CreateNewUserAsync(loggedInUser,
                        new UserModel
                        {
                            UserName = otherUserName,
                            EmailAddress = $"{otherUserName}@test.com"
                        },
                        otherUsersPassword).Result;
                    Assert.True(response.Success);
                }
                catch
                {
                    didThrow = true;
                }

                Assert.False(didThrow);
                Assert.True(dbContext.Users.Any(u => u.UserName == otherUserName));

                var newUser = adminService.AuthenticateUserAsync(new LoginModel
                {
                    LoginName = otherUserName,
                    PassWord = otherUsersPassword
                }).Result;
                Assert.NotNull(newUser);
            }
        }

        [Fact]
        public void UserCannotCreateAnotherUserWithTheSameName()
        {
            var dbOptions = new DbContextOptionsBuilder<FiasmDbContext>()
                .UseInMemoryDatabase(databaseName: "UserCannotCreateAnotherUserWithTheSameName").Options;
            IHasher hasher = new Hasher();

            string userName = "testUser";
            string otherUserName = "testUserOther";
            string password = "P@ssword1";
            string authUserClaim = Core.Models.UserModels.ClaimTypes.AuthorizedUser.ToString();
            string authAdminUserClaim = Core.Models.UserModels.ClaimTypes.AuthorizedToDoUserAdministration.ToString();

            using (var dbContext = new FiasmDbContext(dbOptions))
            {
                var claimAuthUser = AddClaim(dbContext, authUserClaim, "true");
                var claimAuthAdminUser = AddClaim(dbContext, authAdminUserClaim, "true");

                // create logged in user
                var user = AddUser(dbContext, userName, $"test@test.com", password, hasher);
                Assert.NotNull(user);
                AddUserClaims(dbContext, userName, new List<string> { authUserClaim, authAdminUserClaim });

                AdminService adminService = new AdminService(dbContext, hasher);
                var loggedInUser = adminService.AuthenticateUserAsync(new LoginModel
                {
                    LoginName = userName,
                    PassWord = password
                }).Result;
                Assert.NotNull(loggedInUser);

                // try to delete the other user
                var didThrow = false;
                try
                {
                    var response = adminService.CreateNewUserAsync(loggedInUser,
                        new UserModel
                        {
                            UserName = userName,
                            EmailAddress = $"{otherUserName}@test.com"
                        },
                        password).Result;
                    Assert.True(response.Success);
                }
                catch
                {
                    didThrow = true;
                }

                Assert.True(didThrow);
            }
        }

        [Fact]
        public void UserCannotCreateAnotherUserWithTheSameEmail()
        {
            var dbOptions = new DbContextOptionsBuilder<FiasmDbContext>()
                .UseInMemoryDatabase(databaseName: "UserCannotCreateAnotherUserWithTheSameEmail").Options;
            IHasher hasher = new Hasher();

            string userName = "testUser";
            string userEmail = $"{userName}@test.com";
            string otherUserName = "testUserOther";
            string password = "P@ssword1";
            string authUserClaim = Core.Models.UserModels.ClaimTypes.AuthorizedUser.ToString();
            string authAdminUserClaim = Core.Models.UserModels.ClaimTypes.AuthorizedToDoUserAdministration.ToString();

            using (var dbContext = new FiasmDbContext(dbOptions))
            {
                var claimAuthUser = AddClaim(dbContext, authUserClaim, "true");
                var claimAuthAdminUser = AddClaim(dbContext, authAdminUserClaim, "true");

                // create logged in user
                var user = AddUser(dbContext, userName, userEmail, password, hasher);
                Assert.NotNull(user);
                AddUserClaims(dbContext, userName, new List<string> { authUserClaim, authAdminUserClaim });

                AdminService adminService = new AdminService(dbContext, hasher);
                var loggedInUser = adminService.AuthenticateUserAsync(new LoginModel
                {
                    LoginName = userName,
                    PassWord = password
                }).Result;
                Assert.NotNull(loggedInUser);

                // try to delete the other user
                var didThrow = false;
                try
                {
                    var response = adminService.CreateNewUserAsync(loggedInUser,
                        new UserModel
                        {
                            UserName = otherUserName,
                            EmailAddress = userEmail
                        },
                        password).Result;
                    Assert.True(response.Success);
                }
                catch
                {
                    didThrow = true;
                }

                Assert.True(didThrow);
            }
        }
        #endregion

        #region Test GetClaimsAsync
        [Fact]
        public void UserCannotGetClaimsWithoutUserAdminClaim()
        {
            var dbOptions = new DbContextOptionsBuilder<FiasmDbContext>()
                .UseInMemoryDatabase(databaseName: "UserCannotGetClaimsWithoutUserAdminClaim").Options;
            IHasher hasher = new Hasher();

            string userName = "testUser";
            string password = "P@ssword1";
            string authUserClaim = Core.Models.UserModels.ClaimTypes.AuthorizedUser.ToString();

            using (var dbContext = new FiasmDbContext(dbOptions))
            {
                var claimAuthUser = AddClaim(dbContext, authUserClaim, "true");

                // create logged in user
                var user = AddUser(dbContext, userName, $"test@test.com", password, hasher);
                Assert.NotNull(user);
                AddUserClaims(dbContext, userName, new List<string> { authUserClaim });

                AdminService adminService = new AdminService(dbContext, hasher);
                var loggedInUser = adminService.AuthenticateUserAsync(new LoginModel
                {
                    LoginName = userName,
                    PassWord = password
                }).Result;
                Assert.NotNull(loggedInUser);

                // try to create another user
                var didThrow = false;
                try
                {
                    var claims = adminService.GetClaimsAsync(loggedInUser).Result;
                    Assert.NotNull(claims);
                }
                catch
                {
                    didThrow = true;
                }

                Assert.True(didThrow);
            }
        }

        [Fact]
        public void UserCanGetClaimsWithUserAdminClaim()
        {
            var dbOptions = new DbContextOptionsBuilder<FiasmDbContext>()
                .UseInMemoryDatabase(databaseName: "UserCanGetClaimsWithUserAdminClaim").Options;
            IHasher hasher = new Hasher();

            string userName = "testUser";
            string password = "P@ssword1";
            string authUserClaim = Core.Models.UserModels.ClaimTypes.AuthorizedUser.ToString();
            string authAdminUserClaim = Core.Models.UserModels.ClaimTypes.AuthorizedToDoUserAdministration.ToString();

            using (var dbContext = new FiasmDbContext(dbOptions))
            {
                var claimAuthUser = AddClaim(dbContext, authUserClaim, "true");
                var claimAuthAdminUser = AddClaim(dbContext, authAdminUserClaim, "true");

                // create logged in user
                var user = AddUser(dbContext, userName, $"test@test.com", password, hasher);
                Assert.NotNull(user);
                AddUserClaims(dbContext, userName, new List<string> { authUserClaim, authAdminUserClaim });

                AdminService adminService = new AdminService(dbContext, hasher);
                var loggedInUser = adminService.AuthenticateUserAsync(new LoginModel
                {
                    LoginName = userName,
                    PassWord = password
                }).Result;
                Assert.NotNull(loggedInUser);

                // try to delete the other user
                var didThrow = false;
                IEnumerable<ClaimModel> claims = null;
                try
                {
                    claims = adminService.GetClaimsAsync(loggedInUser).Result;
                    Assert.NotNull(claims);
                }
                catch
                {
                    didThrow = true;
                }

                Assert.False(didThrow);
                Assert.True(claims.Count() == 2);
                Assert.Contains(claims, cm => cm.ClaimType == ClaimTypes.AuthorizedUser.ToString());
                Assert.Contains(claims, cm => cm.ClaimType == ClaimTypes.AuthorizedToDoUserAdministration.ToString());
            }
        }

        #endregion

        #region Test AddClaimToUserAsync
        [Fact]
        public void UserCannotAddClaimToUserWithoutUserAdminClaim()
        {
            var dbOptions = new DbContextOptionsBuilder<FiasmDbContext>()
                .UseInMemoryDatabase(databaseName: "UserCannotAddClaimToUserWithoutUserAdminClaim").Options;
            IHasher hasher = new Hasher();

            string userName = "testUser";
            string password = "P@ssword1";
            string otherUserName = userName + "other";
            string authUserClaim = ClaimTypes.AuthorizedUser.ToString();
            string authAdminUserClaim = ClaimTypes.AuthorizedToDoUserAdministration.ToString();

            using (var dbContext = new FiasmDbContext(dbOptions))
            {
                var claimAuthUser = AddClaim(dbContext, authUserClaim, "true");

                // create logged in user
                var user = AddUser(dbContext, userName, $"test@test.com", password, hasher);
                Assert.NotNull(user);
                AddUserClaims(dbContext, userName, new List<string> { authUserClaim });

                // create other user
                var otherUser = AddUser(dbContext, otherUserName, $"testOther@test.com", password, hasher);
                Assert.NotNull(otherUser);
                AddUserClaims(dbContext, otherUserName, new List<string> { authUserClaim });

                AdminService adminService = new AdminService(dbContext, hasher);
                var loggedInUser = adminService.AuthenticateUserAsync(new LoginModel
                {
                    LoginName = userName,
                    PassWord = password
                }).Result;
                Assert.NotNull(loggedInUser);

                // get UserModel of other user
                UserModel otherUserModel = mapper.Map<UserModel>(otherUser);
                Assert.NotNull(otherUserModel);

                // try to deactivate the other user
                var didThrow = false;
                try
                {
                    var response = adminService.AddClaimToUserAsync(loggedInUser, otherUserModel,
                        new ClaimModel
                        {
                            ClaimType = authAdminUserClaim,
                            ClaimValue = "true"
                        }).Result;
                    Assert.True(response.Success);
                }
                catch
                {
                    didThrow = true;
                }

                Assert.True(didThrow);
            }
        }

        [Fact]
        public void UserCannotAddClaimToSelfWithoutUserAdminClaim()
        {
            var dbOptions = new DbContextOptionsBuilder<FiasmDbContext>()
                .UseInMemoryDatabase(databaseName: "UserCannotAddClaimToSelfWithoutUserAdminClaim").Options;
            IHasher hasher = new Hasher();

            string userName = "testUser";
            string password = "P@ssword1";
            string authUserClaim = ClaimTypes.AuthorizedUser.ToString();
            string authAdminUserClaim = ClaimTypes.AuthorizedToDoUserAdministration.ToString();

            using (var dbContext = new FiasmDbContext(dbOptions))
            {
                var claimAuthUser = AddClaim(dbContext, authUserClaim, "true");

                // create logged in user
                var user = AddUser(dbContext, userName, $"test@test.com", password, hasher);
                Assert.NotNull(user);
                AddUserClaims(dbContext, userName, new List<string> { authUserClaim });

                AdminService adminService = new AdminService(dbContext, hasher);
                var loggedInUser = adminService.AuthenticateUserAsync(new LoginModel
                {
                    LoginName = userName,
                    PassWord = password
                }).Result;
                Assert.NotNull(loggedInUser);

                // try to deactivate the other user
                var didThrow = false;
                try
                {
                    var response = adminService.AddClaimToUserAsync(loggedInUser, loggedInUser,
                        new ClaimModel
                        {
                            ClaimType = authAdminUserClaim,
                            ClaimValue = "true"
                        }).Result;
                    Assert.True(response.Success);
                }
                catch
                {
                    didThrow = true;
                }

                Assert.True(didThrow);
            }
        }

        [Fact]
        public void UserCanAddClaimToUserWithUserAdminClaim()
        {
            var dbOptions = new DbContextOptionsBuilder<FiasmDbContext>()
                .UseInMemoryDatabase(databaseName: "UserCanAddClaimToUserWithUserAdminClaim").Options;
            IHasher hasher = new Hasher();

            string userName = "testUser";
            string password = "P@ssword1";
            string otherUserName = userName + "other";
            string authUserClaim = ClaimTypes.AuthorizedUser.ToString();
            string authAdminUserClaim = ClaimTypes.AuthorizedToDoUserAdministration.ToString();

            using (var dbContext = new FiasmDbContext(dbOptions))
            {
                var claimAuthUser = AddClaim(dbContext, authUserClaim, "true");
                var claimAuthUserAdmin = AddClaim(dbContext, authAdminUserClaim, "true");

                // create logged in user
                var user = AddUser(dbContext, userName, $"test@test.com", password, hasher);
                Assert.NotNull(user);
                AddUserClaims(dbContext, userName, new List<string> { authUserClaim, authAdminUserClaim });

                // create other user
                var otherUser = AddUser(dbContext, otherUserName, $"testOther@test.com", password, hasher);
                Assert.NotNull(otherUser);
                AddUserClaims(dbContext, otherUserName, new List<string> { authUserClaim });

                AdminService adminService = new AdminService(dbContext, hasher);
                var loggedInUser = adminService.AuthenticateUserAsync(new LoginModel
                {
                    LoginName = userName,
                    PassWord = password
                }).Result;
                Assert.NotNull(loggedInUser);

                // get UserModel of other user
                UserModel otherUserModel = mapper.Map<UserModel>(otherUser);
                Assert.NotNull(otherUserModel);

                // try to deactivate the other user
                var didThrow = false;
                try
                {
                    var response = adminService.AddClaimToUserAsync(loggedInUser, otherUserModel,
                        new ClaimModel
                        {
                            ClaimType = authAdminUserClaim,
                            ClaimValue = "true"
                        }).Result;
                    Assert.True(response.Success);
                }
                catch
                {
                    didThrow = true;
                }

                Assert.False(didThrow);
                var dbuserClaims = dbContext.Users.FirstOrDefault(u => u.UserName == otherUserName)?.UserClaims.ToList();
                Assert.True(dbuserClaims.Count == 2);
            }
        }
        #endregion

        #region Test RemoveClaimFromUserAsync
        [Fact]
        public void UserCannotRemoveClaimFromOthersWithoutUserAdminClaim()
        {
            var dbOptions = new DbContextOptionsBuilder<FiasmDbContext>()
                .UseInMemoryDatabase(databaseName: "UserCannotRemoveClaimFromOthersWithoutUserAdminClaim").Options;
            IHasher hasher = new Hasher();

            string userName = "testUser";
            string password = "P@ssword1";
            string otherUserName = userName + "other";
            string authUserClaim = ClaimTypes.AuthorizedUser.ToString();
            string authAdminUserClaim = ClaimTypes.AuthorizedToDoUserAdministration.ToString();

            using (var dbContext = new FiasmDbContext(dbOptions))
            {
                var claimAuthUser = AddClaim(dbContext, authUserClaim, "true");
                var claimAuthAdminUser = AddClaim(dbContext, authAdminUserClaim, "true");

                // create logged in user
                var user = AddUser(dbContext, userName, $"test@test.com", password, hasher);
                Assert.NotNull(user);
                AddUserClaims(dbContext, userName, new List<string> { authUserClaim });

                // create other user
                var otherUser = AddUser(dbContext, otherUserName, $"testOther@test.com", password, hasher);
                Assert.NotNull(otherUser);
                AddUserClaims(dbContext, otherUserName, new List<string> { authUserClaim, authAdminUserClaim });

                AdminService adminService = new AdminService(dbContext, hasher);
                var loggedInUser = adminService.AuthenticateUserAsync(new LoginModel
                {
                    LoginName = userName,
                    PassWord = password
                }).Result;
                Assert.NotNull(loggedInUser);

                // get UserModel of other user
                UserModel otherUserModel = mapper.Map<UserModel>(otherUser);
                Assert.NotNull(otherUserModel);

                // try to remove claim from other user
                var didThrow = false;
                try
                {
                    var response = adminService.RemoveClaimFromUserAsync(loggedInUser, otherUserModel,
                        new ClaimModel
                        {
                            ClaimType = authAdminUserClaim,
                            ClaimValue = "true"
                        }).Result;
                    Assert.True(response?.Success ?? false);
                }
                catch
                {
                    didThrow = true;
                }

                Assert.True(didThrow);
            }
        }

        [Fact]
        public void UserCannotRemoveUserAdminClaimFromSelf()
        {
            var dbOptions = new DbContextOptionsBuilder<FiasmDbContext>()
                .UseInMemoryDatabase(databaseName: "UserCannotRemoveUserAdminClaimFromSelf").Options;
            IHasher hasher = new Hasher();

            string userName = "testUser";
            string password = "P@ssword1";
            string authUserClaim = ClaimTypes.AuthorizedUser.ToString();
            string authAdminUserClaim = ClaimTypes.AuthorizedToDoUserAdministration.ToString();

            using (var dbContext = new FiasmDbContext(dbOptions))
            {
                var claimAuthUser = AddClaim(dbContext, authUserClaim, "true");
                var claimAuthAdminUser = AddClaim(dbContext, authAdminUserClaim, "true");

                // create logged in user
                var user = AddUser(dbContext, userName, $"test@test.com", password, hasher);
                Assert.NotNull(user);
                AddUserClaims(dbContext, userName, new List<string> { authUserClaim });

                AdminService adminService = new AdminService(dbContext, hasher);
                var loggedInUser = adminService.AuthenticateUserAsync(new LoginModel
                {
                    LoginName = userName,
                    PassWord = password
                }).Result;
                Assert.NotNull(loggedInUser);

                // try to remove claim from other user
                var didThrow = false;
                try
                {
                    var response = adminService.RemoveClaimFromUserAsync(loggedInUser, loggedInUser,
                        new ClaimModel
                        {
                            ClaimType = authAdminUserClaim,
                            ClaimValue = "true"
                        }).Result;
                    Assert.True(response?.Success ?? false);
                }
                catch
                {
                    didThrow = true;
                }

                Assert.True(didThrow);
            }
        }

        [Fact]
        public void UserCanRemoveClaimFromOthersWithUserAdminClaim()
        {
            var dbOptions = new DbContextOptionsBuilder<FiasmDbContext>()
                .UseInMemoryDatabase(databaseName: "UserCanRemoveClaimFromOthersWithUserAdminClaim").Options;
            IHasher hasher = new Hasher();

            string userName = "testUser";
            string password = "P@ssword1";
            string otherUserName = userName + "other";
            string authUserClaim = ClaimTypes.AuthorizedUser.ToString();
            string authAdminUserClaim = ClaimTypes.AuthorizedToDoUserAdministration.ToString();

            using (var dbContext = new FiasmDbContext(dbOptions))
            {
                var claimAuthUser = AddClaim(dbContext, authUserClaim, "true");
                var claimAuthAdminUser = AddClaim(dbContext, authAdminUserClaim, "true");

                // create logged in user
                var user = AddUser(dbContext, userName, $"test@test.com", password, hasher);
                Assert.NotNull(user);
                AddUserClaims(dbContext, userName, new List<string> { authUserClaim, authAdminUserClaim });

                // create other user
                var otherUser = AddUser(dbContext, otherUserName, $"testOther@test.com", password, hasher);
                Assert.NotNull(otherUser);
                AddUserClaims(dbContext, otherUserName, new List<string> { authUserClaim, authAdminUserClaim });

                AdminService adminService = new AdminService(dbContext, hasher);
                var loggedInUser = adminService.AuthenticateUserAsync(new LoginModel
                {
                    LoginName = userName,
                    PassWord = password
                }).Result;
                Assert.NotNull(loggedInUser);

                // get UserModel of other user
                UserModel otherUserModel = mapper.Map<UserModel>(otherUser);
                Assert.NotNull(otherUserModel);

                // try to remove claim from other user
                var didThrow = false;
                try
                {
                    var response = adminService.RemoveClaimFromUserAsync(loggedInUser, otherUserModel,
                        new ClaimModel
                        {
                            ClaimType = authAdminUserClaim,
                            ClaimValue = "true"
                        }).Result;
                    Assert.True(response?.Success ?? false);
                }
                catch
                {
                    didThrow = true;
                }

                Assert.False(didThrow);
                Assert.DoesNotContain(dbContext.Users.Single(u => u.UserName == otherUserName).UserClaims,
                    uc => uc.Claim.ClaimType == authAdminUserClaim);
            }
        }
        #endregion

    }
}
