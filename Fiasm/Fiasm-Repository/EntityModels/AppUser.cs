using AutoMapper;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Text;

namespace Fiasm.Repository.EntityModels
{
    // Implements IdentityUser for .Net Identity implementation
    public class AppUser
    {
        public int AppUserId { get; set; }

        public string LoginName { get; set; }
        public string HashedPassword { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public bool IsActive { get; set; }

        public virtual ICollection<AppUserClaim> AppUserClaims { get; set; }
    }
}
