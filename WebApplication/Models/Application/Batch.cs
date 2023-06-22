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
    public class Batch
    {


        public Batch()
        {
            this.Date = DateTime.Now;
        }

        public Batch(string UserId) : this()
        {
            this.UserId = UserId;
        }

        [Required]
        [DisplayName("Id")]
        [FilterField]
        public int Id { get; set; }


        [DisplayName("Model")]
        public int? ModelId { get; set; }

        [ForeignKey("ModelId")]
        public virtual Model Model { get; set; }


        [DisplayName("Quantity")]
        public int Quantity { get; set; }


        [DisplayName("Quantity Conferred")]
        public int QuantityConferred { get; set; }

        [Required]
        [DisplayName("Date")]
        [DataType("DateTime")]
        public DateTime Date { get; set; }

        [Required]
        [DisplayName("User")]
        [FilterField]
        public string UserId { get; set; }


        [DisplayName("Quantity Approved")]
        public int? QuantityApproved { get; set; }


        [DisplayName("Quantity Disapproved by Functional Test")]
        public int? QuantityDisapproved { get; set; }


        [DisplayName("Quantity Disapproved By Cosmestic")]
        public int? QuantityDisapprovedByCosmetic { get; set; }

        [DisplayName("Functional Test Date")]
        [DataType("DateTime")]
        public DateTime? FunctionalTestDate { get; set; }

        [DisplayName("Functional Test User")]
        public string FunctionalTestUserId { get; set; }


        [DisplayName("Status")]
        [FilterField]
        public BatchStatus Status { get; set; }


        public bool IsQuantitiesOk()
        {
            return QuantityConferred == QuantityApproved + QuantityDisapproved + QuantityDisapprovedByCosmetic;
        }

        public bool IsDivergent()
        {
            return Quantity != QuantityConferred;
        }

        public bool IsDivergentByApproved()
        {
            return Quantity != QuantityConferred && (Status == BatchStatus.Tested || Status == BatchStatus.Conferred);
        }

        public string GetDivergentCssClass()
        {
            if (IsDivergentByApproved())
            {
                return "Status No-Divergent";
            }
            else if (IsDivergent())
            {
                return "Status Divergent";
            }
            else
            {
                return "Status No-Divergent";
            }
        }

        public string GetDivergentVerbose()
        {
            if (IsDivergentByApproved())
            {
                return "Yes But Approved";
            }
            else
            if (IsDivergent())
            {
                return "Yes";
            }
            else
            {
                return "No";
            }
        }


        public ICollection<BatchItem> BatchItems { get; set; }

        public ICollection<BatchItem> BatchItemsConferred { get; set; }

        public ICollection<BatchItem> BatchItemsNotConferred { get; set; }


        public ICollection<BatchItem> BatchItemsFound { get; set; }

        public ICollection<BatchItem> BatchItemsNotFound { get; set; }


        public string GetCode()
        {
            return Id.ToString() + "-" + this.Date.ToString("yyyyMMddHHmmss");
        }

    }

    public enum BatchStatus
    {
        [Description("Pending Review")]
        PendingReview,

        [Description("Conferred")]
        Conferred,

        [Description("Tested")]
        Tested,

        [Description("Invalid")]
        Invalid,

        [Description("Deleted")]
        Deleted
    }
}
