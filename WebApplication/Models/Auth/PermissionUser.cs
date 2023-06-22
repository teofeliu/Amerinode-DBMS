using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace WebApplication.Models.Auth
{
    public class PermissionUser
    {
        [Key]
        [Display(Name = "Id")]
        public int Id { get; set; }

        [Index("IX_UserPerm_Resource")]
        [Index("UQ_UserPerm", 1, IsUnique = true)]
        [Required]
        [MaxLength(128)]
        [Display(Name = "Resource")]
        public string Resource { get; set; }

        [Index("UQ_UserPerm", 2, IsUnique = true)]
        [MaxLength(32)]
        [Display(Name = "Operation")]
        public string Operation { get; set; }

        [Index("IX_UserPerm_UserId")]
        [Index("UQ_UserPerm", 3, IsUnique = true)]
        [Required]
        [MaxLength(128)]
        [Display(Name = "User Id")]
        public string UserId { get; set; }
    }
}