using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using WebApplication.Attributes;


namespace WebApplication.Models.Application {

    public class Manufacturer
    {
        [Required]
        [DisplayName("Id")]
        public int Id { get; set; }

        [Required]
        [DisplayName("Name")]
        [FilterField]
        public string Name { get; set; }

        public ICollection<Model> Models { get; set; }

    }

}