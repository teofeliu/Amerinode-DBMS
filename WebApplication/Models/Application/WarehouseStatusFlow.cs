
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace WebApplication.Models.Application
{
    /// <summary>
    /// This model stores the historical flow changes related to status changed and
    /// correct warehouse destination.
    /// </summary>
    public class WarehouseStatusFlow
    {
        [Key]
        [Required]
        [DisplayName("ID")]
        public int Id { get; set; }

        [Required]
        [DisplayName("Current Status")]
        public RequestFlowStatus RequestFlowStatus { get; set; }

        [Required]
        [DisplayName("Origin Warehouse")]
        public string OriginId { get; set; }
        [ForeignKey("OriginId")]
        public virtual Warehouse Origin { get; set; }

        [Required]
        [DisplayName("Destination Warehosue")]
        public string DestinationId { get; set; }
        [ForeignKey("DestinationId")]
        public virtual Warehouse Destination { get; set; }

        [Required]
        [DisplayName("Date")]
        public DateTime Date { get; set; }

        [Required]
        [DisplayName("User who execute the operation")]
        public string UserId { get; set; }

        [DisplayName("Description")]
        public string Description { get; set; }

        [Required]
        [DisplayName("Refurb Request")]
        public int RequestId { get; set; }

        [ForeignKey("RequestId")]
        public RefurbRequest RefurbRequest { get; set; }
    }
}