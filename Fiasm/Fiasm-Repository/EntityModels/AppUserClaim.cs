using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
using System;
using System.Collections.Generic;
using System.Text;
using AutoMapper;

namespace Fiasm.Repository.EntityModels
{
    public class AppUserClaim 
    {
        public int AppUserClaimId { get; set; }

        public string AppUserClaimValue { get; set; }

        public virtual AppClaim AppClaim { get; set; }
    }
}
