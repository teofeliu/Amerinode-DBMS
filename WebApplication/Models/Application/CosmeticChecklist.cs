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
    public class CosmeticChecklist
    {

        public CosmeticChecklist()
        {
        }
        public CosmeticChecklist(int RequestId, int CosmeticOverviewId)
        {
            this.RequestId = RequestId;
            this.CosmeticOverviewId = CosmeticOverviewId;
        }


        [Required]
        [DisplayName("Id")]
        public int Id { get; set; }

        [Required]
        [DisplayName("Cosmetic Overview")]
        public int CosmeticOverviewId { get; set; }

        [ForeignKey("CosmeticOverviewId")]
        public virtual CosmeticOverview CosmeticOverview { get; set; }

        [Required]
        [DisplayName("Request")]
        public int RequestId { get; set; }

        [ForeignKey("RequestId")]
        public virtual RefurbRequest Request { get; set; }
    }
}