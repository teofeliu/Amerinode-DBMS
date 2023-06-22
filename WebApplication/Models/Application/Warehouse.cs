using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using WebApplication.Attributes;

namespace WebApplication.Models.Application
{
    public class Warehouse
    {
        [DisplayName("ID")]
        [Key]
        [FilterField]
        [Required]
        public string Id { get; set; }

        [DisplayName("Name")]
        [Required]
        [FilterField]
        public string Name { get; set; }

        [DisplayName("Type")]
        public WarehouseType WarehouseType { get; set; }

        public override string ToString()
        {
            return String.Format("{0:D4} - {1}", Id, Name);
        }
    }
}