using System;
using System.Collections.Generic;
using System.Text;

namespace Fiasm.Core.Models.UserModels
{
    public class ClaimModel
    {
        public string ClaimType { get; set; }
        public string ClaimValue { get; set; }
    }

    internal enum ClaimTypes
    {
        AuthorizedToDoUserAdministration
    }
}
