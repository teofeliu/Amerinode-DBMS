using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebApplication.Models.Application
{

    public class BatchItem
    {

        public BatchItem()
        {
            this.Date = DateTime.Now;
        }

        [Required]
        [DisplayName("Id")]
        public int Id { get; set; }

        [Required]
        [DisplayName("Batch")]
        public int BatchId { get; set; }

        [ForeignKey("BatchId")]
        public virtual Batch Batch { get; set; }

        [DisplayName("Model")]
        public int? ModelId { get; set; }

        [ForeignKey("ModelId")]
        public virtual Model Model { get; set; }

        [DisplayName("Serial Number")]
        public string SerialNumber { get; set; }


        [DisplayName("Conferred")]
        public bool Conferred { get; set; }

        [DisplayName("Found")]
        public bool Found { get; set; }

        [DisplayName("Date Conferred")]
        public DateTime? DateConferred { get; set; }

        [Required]
        [DisplayName("Date Arrived")]
        [DataType("DateTime")]
        public DateTime Date { get; set; }

        [DisplayName("Warehouse of origin")]
        public string OriginId { get; set; }

        [ForeignKey("OriginId")]
        public virtual Warehouse Origin { get; set; }

        [DisplayName("Warehouse of destination")]
        public string DestinationId { get; set; }

        [ForeignKey("DestinationId")]
        public virtual Warehouse Destination { get; set; }

        [DisplayName("Invoice")]
        public string Invoice { get; set; }
    }

}
