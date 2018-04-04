using System;
using System.Collections.Generic;
using System.Text;

namespace Fiasm.Data.EntityModels
{
    public class UserClaim
    {
        public int UserClaimId { get; set; }
        public int UserId { get; set; }
        public int ClaimId { get; set; }

        // Foreign keys
        public User User{ get; set; }
        public Claim Claim { get; set; }
    }
}
