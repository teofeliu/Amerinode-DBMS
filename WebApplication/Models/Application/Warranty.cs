using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using WebApplication.Attributes;

namespace WebApplication.Models.Application
{
    public class Warranty
    {
        [Required]
        [DisplayName("Id")]
        public int Id { get; set; }

        [Required]
        [DisplayName("Request")]
        public int RequestId { get; set; }

        [ForeignKey("RequestId")]
        public virtual RefurbRequest Request { get; set; }

        [Required]
        [DisplayName("In Warranty?")]
        public bool InWarranty { get; set; }

        [DisplayName("Description")]
        public string Description { get; set; }
    }
}
