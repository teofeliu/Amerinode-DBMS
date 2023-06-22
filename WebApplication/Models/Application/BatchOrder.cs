using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


namespace WebApplication.Models.Application
{
    [Table("BatchOrder")]
    public class BatchOrder
    {

        public BatchOrder()
        {
            this.DateCreate = DateTime.Now;
            this.UserId = UserId;
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
        [DisplayName("Quantity")]
        public int Quantity { get; set; }

        [Required]
        [DisplayName("Date Create")]
        [DataType("DateTime")]
        public DateTime DateCreate { get; set; }
        public string UserId { get; set; }

    }
}