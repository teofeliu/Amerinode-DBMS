using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace WebApplication.Models.Application
{
    public class ScrapBatchItem
    {
        [Key]
        [Required]
        [Display(Name = "Id")]
        public int Id { get; set; }

        [Required]
        [Display(Name = "Scrap Batch")]
        public int ScrapBatchId { get; set; }


        [ForeignKey("ScrapBatchId")]
        public virtual ScrapBatch ScrapBatch { get; set; }

        [Required]
        [Display(Name = "Refurb Request")]
        public int RefurbRequestId { get; set; }

        [ForeignKey("RefurbRequestId")]
        public virtual RefurbRequest RefurbRequest { get; set; }

        [Required]
        [Display(Name = "Created at")]
        public DateTime Date { get; set; }

        [Required]
        [Display(Name = "User")]
        public string UserId { get; set; }

        [DisplayName("Warehouse of origin")]
        public string OriginId { get; set; }

        [ForeignKey("OriginId")]
        public virtual Warehouse Origin { get; set; }

        [DisplayName("Warehouse of destination")]
        public string DestinationId { get; set; }

        [ForeignKey("DestinationId")]
        public virtual Warehouse Destination { get; set; }


        public Dictionary<string, object> ToJson() {
            var dict = new Dictionary<string, object>();
            dict.Add("Id", Id);
            dict.Add("SerialNumber", RefurbRequest.SerialNumber);
            dict.Add("Model", RefurbRequest.Model.Name);
            dict.Add("Date", Date);
            dict.Add("UserId", UserId);
            dict.Add("Origin", Origin.ToString());
            dict.Add("Destination", Destination.ToString());

            return dict;
        }
    }
}