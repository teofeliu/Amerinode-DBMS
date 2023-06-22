using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using WebApplication.Attributes;

namespace WebApplication.Models.Application
{
    public class Contact
    {
        [Required]
        [DisplayName("Id")]
        public int Id { get; set; }

        [Required]
        [FilterField]
        [DisplayName("Email")]
        public string Email { get; set; }
    }
}
