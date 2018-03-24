using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Fiasm.Data.EntityModels
{
    public class UserChangeLog
    {
        public int UserChangeLogId { get; set; }
        [ForeignKey("User")]
        public int ChangedUserId { get; set; }
        public string ChangeDesc { get; set; }
        public DateTime ModifiedOn { get; set; }
        [ForeignKey("User")]
        public int ChangedByUserId { get; set; }
    }
}
