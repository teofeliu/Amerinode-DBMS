using Microsoft.AspNet.Identity.EntityFramework;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace WebApplication.Models.Auth
{
    public class PermissionRole
    {
        [Key]
        [Display(Name="Id")]
        public int Id { get; set; }

        [Index("IX_RolePerm_Resource")]
        [Index("UQ_RolePerm", 1, IsUnique = true)]
        [Required]
        [MaxLength(128)]
        [Display(Name="Resource")]
        public string Resource { get; set; }

        [Index("UQ_RolePerm", 2, IsUnique = true)]
        [MaxLength(32)]
        [Display(Name="Operation")]
        public string Operation { get; set; }

        [Index("IX_RolePerm_Role")]
        [Index("UQ_RolePerm", 3, IsUnique = true)]
        [Required]
        [MaxLength(128)]
        [Display(Name = "Role assigned")]
        public string RoleName { get; set; }
    }

    public class PermissionRoleViewModel
    {
        public string RoleName { get; set; }
        public string Resource { get; set; }
        public string Operation { get; set; }
        public bool IsSelected { get; set; }
    }
}