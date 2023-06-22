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
    public class DOA
    {


        public DOA()
        {
            //this.Date = DateTime.Now;
        }

        public DOA(string ImportId, string TM, string Model, string Manufacturer, string SerialNumber, string Invoice, DateTime DateReceived)
        {
            this.ImportId = ImportId;
            this.TM = TM;
            this.Model = Model;
            this.Manufacturer = Manufacturer;
            this.SerialNumber = SerialNumber;
            this.Invoice = Invoice;
            this.DateReceived = DateReceived;
        }

        [Required]
        [DisplayName("Id")]
        [FilterField]
        public int Id { get; set; }

        [Required]
        [DisplayName("Import ID")]
        public string ImportId { get; set; }

        [DisplayName("TM")]
        public string TM { get; set; }

        [DisplayName("Model")]
        public string Model { get; set; }

        [DisplayName("Manufacturer")]
        public string Manufacturer { get; set; }

        [FilterField]
        [DisplayName("Serial Number")]
        public string SerialNumber { get; set; }

        [DisplayName("Invoice")]
        public string Invoice { get; set; }

        [Required]
        [DisplayName("Date Received")]
        [DataType("DateTime")]
        public DateTime DateReceived { get; set; }
    }
}
