using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using WebApplication.Attributes;

namespace WebApplication.Models.Application
{
    public class BatchStock
    {

        public BatchStock()
        {
            this.Date = DateTime.Now;
        }

        public BatchStock(string UserId) : this()
        {
            this.UserId = UserId;
        }

        [Key]
        [Required]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [DisplayName("Id")]        
        public int Id { get; set; }

        [Required]
        [DisplayName("Nº Request")]
        [FilterField]
        public string NumeroNota { get; set; }

        [Required]
        [DisplayName("Date")]
        [DataType("DateTime")]
        public DateTime Date { get; set; }

        [Required]
        [DisplayName("User")]
        public string UserId { get; set; }
    }
}