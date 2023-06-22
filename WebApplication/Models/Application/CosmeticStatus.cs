using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using WebApplication.Attributes;


namespace WebApplication.Models.Application {

    public class CosmeticStatus
    {
        [Required]
        [DisplayName("Id")]
        public int Id { get; set; }

        [Required]
        [DisplayName("Name")]
        [FilterField]
        public string Name { get; set; }
    }

}