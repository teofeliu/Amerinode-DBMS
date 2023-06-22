using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using WebApplication.Attributes;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebApplication.Models.Application
{
    public class TrialViewModel
    {
        public TrialViewModel()
        {
            this.Date = DateTime.Now;
        }

        public TrialViewModel(string UserId, int RequestId) : this()
        {
            this.UserId = UserId;
            this.RequestId = RequestId;
        }

        [Required]
        [DisplayName("Id")]
        public int Id { get; set; }


        [Required]
        [DisplayName("Request")]
        public int RequestId { get; set; }

        [ForeignKey("RequestId")]
        public virtual RefurbRequest Request { get; set; }


        [Required]
        [DataType(DataType.MultilineText)]
        [DisplayName("Description")]
        public string Description { get; set; }

        [Required]
        [DisplayName("Date")]
        [DataType("DateTime")]
        public DateTime Date { get; set; }

        [Required]
        [DisplayName("User")]
        public string UserId { get; set; }

        [Required]
        [DisplayName("In Warranty?")]
        public bool InWarranty { get; set; }

        [Required(AllowEmptyStrings = false,
            ErrorMessage = "A description is required as to why the item is not fit for warranty.")]
        [DisplayName("If not, why?")]
        [DataType(DataType.MultilineText)]
        public string WarrantyDescription { get; set; }

        public virtual ICollection<CosmeticChecklist> CosmeticChecklist { get; set; }
    }
}
