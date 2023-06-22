
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using WebApplication.Attributes;

namespace WebApplication.Models.Application
{
    public class Accessories
    {

        public Accessories(){}

        [Required]
        [DisplayName("Id")]
        [FilterField]
        int id { get; set; }

        [DisplayName("Name")]
        string name { get; set; }

        [DisplayName("Description")]
        string description { get; set; }
    }
}