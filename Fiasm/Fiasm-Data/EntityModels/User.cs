using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Fiasm.Data.EntityModels
{
    // Implements IdentityUser for .Net Identity implementation
    public class User
    {
        public int UserId { get; set; }
        [Required, MaxLength(64)]
        public string UserName { get; set; }
        [Required, MaxLength(128)]
        public string HashedPassword { get; set; }
        [Required, EmailAddress, MaxLength(64)]
        public string EmailAddress { get; set; }
        [Required, Phone, MaxLength(32)]
        public string PhoneNumber { get; set; }
        [Required, DefaultValue(false)]
        public bool IsPasswordReseting { get; set; }
        public bool IsPasswordResetRequired { get; set; }
        [MaxLength(128)]
        public string ResettingPasswordUUID { get; set; }
        [Required]
        public bool IsActive { get; set; }

        // Foreign keys
        public ICollection<UserClaim> UserClaims { get; set; }
        public User()
        {
            UserClaims = new HashSet<UserClaim>();
        }
    }
}
