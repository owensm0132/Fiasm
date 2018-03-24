using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Fiasm.Data.EntityModels
{
    public class Claim
    {
        public int ClaimId { get; set; }
        [Required, StringLength(64)]
        public string ClaimType { get; set; }
        [Required, StringLength(64)]
        public string ClaimValue { get; set; }

        // Foreign keys
        public ICollection<User> Users { get; set; }
        public Claim()
        {
            Users = new HashSet<User>();
        }
    }
}
/*
User 1<=>0.* Claims
Standard 1<=>0.* Student




*/
