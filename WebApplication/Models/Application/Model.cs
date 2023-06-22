using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using WebApplication.Attributes;
using System.ComponentModel.DataAnnotations.Schema;
using System.Web.Mvc;

namespace WebApplication.Models.Application
{

    public class ModelType
    {
        [Required]
        [DisplayName("Id")]
        public int Id { get; set; }

        [Required]
        [DisplayName("Name")]
        [FilterField]
        public string Name { get; set; }
        public bool Controlstock { get; set; }
        public ICollection<Model> Models { get; set; }
    }

    public class Model
    {
        [Required]
        [DisplayName("Id")]
        public int Id { get; set; }

        [FilterField]
        [DisplayName("Part Number")]
        public string PartNumber { get; set; }

        [DisplayName("Cód. SAP")]
        public string TM { get; set; }

        [Required]
        [FilterField]
        [DisplayName("Name")]
        public string Name { get; set; }

        [DataType(DataType.MultilineText)]
        [DisplayName("Description")]
        public string Description { get; set; }


        [DisplayName("Manufacturer")]
        public int ? ManufacturerId { get; set; }

        [ForeignKey("ManufacturerId")]
        public virtual Manufacturer Manufacturer { get; set; }

        public IEnumerable<SelectListItem> ManufacturerList { get; set; }

        [DisplayName("Type")]
        public int ModelTypeId { get; set; }

        [ForeignKey("ModelTypeId")]
        public virtual ModelType Type { get; set; }

        public IEnumerable<SelectListItem> TypeList { get; set; }

        [DisplayName("Voltage")]
        public int ? Voltage { get; set; }

        [DisplayName("Current")]
        public Decimal ? Current { get; set; }

        public ICollection<RefurbRequest> RefurbRequests { get; set; }
        
        [DisplayName("Batch Stock")]
        public int Stock { get; set; }

    }

}
