using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Fiasm.Core.Models.UserModels
{
    public class LoginModel
    {
        [Required, StringLength(64)]
        public string LoginName { get; set; }
        [Required, StringLength(64)]
        public string PassWord { get; set; }
    }

    public class ResetLoginModel
    {
        [Required, EmailAddress, StringLength(64)]
        public string EmailAddress { get; set; }
        [Required, StringLength(128)]
        public string UUID { get; set; }
        [Required, StringLength(64)]
        public string NewPassword { get; set; }
    }
}
