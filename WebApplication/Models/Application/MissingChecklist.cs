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
    public class MissingChecklist
    {

        public MissingChecklist()
        {
        }
        public MissingChecklist(int RequestId, int HardwareOverviewId)
        {
            this.RequestId = RequestId;
            this.HardwareOverviewId = HardwareOverviewId;
        }


        [Required]
        [DisplayName("Id")]
        public int Id { get; set; }

        [Required]
        [DisplayName("HardwareOverview")]
        public int HardwareOverviewId { get; set; }

        [ForeignKey("HardwareOverviewId")]
        public virtual HardwareOverview HardwareOverview { get; set; }

        [Required]
        [DisplayName("Request")]
        public int RequestId { get; set; }

        [ForeignKey("RequestId")]
        public virtual RefurbRequest Request { get; set; }
    }

}