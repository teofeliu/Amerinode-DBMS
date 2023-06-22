using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


namespace WebApplication.Models.Application
{
    public class BatchProducts
    {

        public BatchProducts()
        {
            this.DateCreate = DateTime.Now;
        }

        [Key]
        [Required]
        [DisplayName("ID")]
        public int Id { get; set; }

        [DisplayName("Model")]
        public int? ModelId { get; set; }

        [ForeignKey("ModelId")]
        public virtual Model Model { get; set; }

        [Required]
        [DisplayName("Date Create")]
        [DataType("DateTime")]
        public DateTime DateCreate { get; set; }

        [DisplayName("Quantity")]
        public int Stock { get; set; }

        [DisplayName("N Request")]
        public int? BatchStockId { get; set; }

        [ForeignKey("BatchStockId")]
        public virtual BatchStock BatchStock { get; set; }
    }
}