using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Fiasm.Core.Models.UserModels
{
    public class ClaimModel
    {
        [StringLength(64)]
        public string ClaimType { get; set; }
        [StringLength(64)]
        public string ClaimValue { get; set; }
    }

    public enum ClaimTypes
    {
        AuthorizedToDoUserAdministration
    }
}
