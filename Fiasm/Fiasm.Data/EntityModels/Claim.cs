using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Fiasm.Data.EntityModels
{
    public class Claim
    {
        public int ClaimId { get; set; }
        [Required, MaxLength(64)]
        public string ClaimType { get; set; }
        [Required, MaxLength(64)]
        public string ClaimValue { get; set; }

        // Foreign keys
        public ICollection<UserClaim> UserClaims { get; set; }
        public Claim()
        {
            UserClaims = new HashSet<UserClaim>();
        }
    }
}
/*
User 1<=>0.* Claims
Standard 1<=>0.* Student




*/
