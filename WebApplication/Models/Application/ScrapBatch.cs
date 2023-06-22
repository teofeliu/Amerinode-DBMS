using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace WebApplication.Models.Application
{
    public class ScrapBatch
    {
        [Key]
        [Required]
        [Display(Name = "ID")]
        public int Id { get; set; }

        [Required]
        [Display(Name = "Created at")]
        public DateTime Created { get; set; }

        [Display(Name = "Discarted at")]
        public DateTime? Delivered { get; set; }

        [Display(Name = "Canceled at")]
        public DateTime? Cancelled { get; set; }

        [Required]
        [Display(Name = "User who created batch")]
        public string UserId { get; set; }

        [Display(Name = "User who processed batch")]
        public string UserDeliveryId { get; set; }

        [Display(Name = "User who canceled batch")]
        public string UserCancelId { get; set; }

        [Required]
        [Display(Name = "Status")]
        public ScrapBatchStatus Status { get; set; }


        public ICollection<ScrapBatchItem> Items { get; set; }


        public string GetCode
        {
            get
            {
                return String.Format("{0}-{1:yyyyMMddHHmmss}", Id, Created);
            }
        }
    }

    public enum ScrapBatchStatus
    {
        [Description("Opened")]
        Open,
        [Description("Delivered")]
        Delivered,
        [Description("Cancelled")]
        Cancelled
    }
}