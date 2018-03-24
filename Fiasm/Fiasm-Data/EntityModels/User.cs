using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Fiasm.Data.EntityModels
{
    // Implements IdentityUser for .Net Identity implementation
    public class User
    {
        public int UserId { get; set; }
        [Required, StringLength(64)]
        public string LoginName { get; set; }
        [Required, StringLength(64)]
        public string HashedPassword { get; set; }
        [Required, EmailAddress]
        public string Email { get; set; }
        [Required, Phone]
        public string Phone { get; set; }
        [Required, DefaultValue(false)]
        public bool IsPasswordReseting { get; set; }
        [Required]
        public bool IsActive { get; set; }

        // Foreign keys
        public ICollection<Claim> Claims { get; set; }
        public User()
        {
            Claims = new HashSet<Claim>();
        }


    }
}
