using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Fiasm.Data.EntityModels
{
    public class UserChangeLog
    {
        private string changeDesc;

        public int UserChangeLogId { get; set; }
        [ForeignKey("User")]
        public int ChangedUserId { get; set; }
        [Required, MaxLength(128)]
        public string ChangeDesc
        {
            get { return changeDesc; }
            set { changeDesc = value.Clamp(128); }
        }
        [Required]
        public DateTime ModifiedOn { get; set; }
        [Required, ForeignKey("User")]
        public int ChangedByUserId { get; set; }
    }
}
