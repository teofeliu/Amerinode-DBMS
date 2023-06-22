using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using WebApplication.Attributes;

namespace WebApplication.Models.Application
{
    public class Supply
    {
        [Required]
        [DisplayName("Id")]
        public int Id { get; set; }

        [Required]
        [FilterField]
        [DisplayName("Name")]
        public string Name { get; set; }

        [Required]
        [DataType(DataType.MultilineText)]
        [DisplayName("Description")]
        public string Description { get; set; }
    }
}
