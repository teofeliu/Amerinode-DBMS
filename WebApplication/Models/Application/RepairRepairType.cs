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

    public class RepairRepairType
    {

        public RepairRepairType()
        {
        }

        public RepairRepairType(int TrialId, int RepairTypeId)
        {
            this.RepairTypeId = RepairTypeId;
            this.RepairId = TrialId;
        }

        public RepairRepairType(RepairType RepairType)
        {
            this.RepairType = RepairType;
            this.RepairTypeId = RepairType.Id;
        }

        [Required]
        [DisplayName("Id")]
        public int Id { get; set; }

        [Required]
        [DisplayName("Repair")]
        public int RepairId { get; set; }

        [ForeignKey("RepairId")]
        public virtual Repair Repair { get; set; }

        [Required]
        [DisplayName("RepairType")]
        public int RepairTypeId { get; set; }

        [ForeignKey("RepairTypeId")]
        public virtual RepairType RepairType { get; set; }
    }

}
