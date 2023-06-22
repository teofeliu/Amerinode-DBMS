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
    public class TrialFunctionalTest
    {
        public TrialFunctionalTest() { }
        public TrialFunctionalTest(int TrialId, int FunctionalTestId)
        {
            this.FunctionalTestId = FunctionalTestId;
            this.TrialId = TrialId;
        }

        public TrialFunctionalTest(TrialFunctionalTest tft)
        {
            this.FunctionalTestId = tft.FunctionalTestId;
            this.TrialId = tft.TrialId;
        }

        [Required]
        [DisplayName("Id")]
        public int Id { get; set; }

        [Required]
        [DisplayName("Trial")]
        public int TrialId { get; set; }

        [ForeignKey("TrialId")]
        public virtual Trial Trial { get; set; }

        [Required]
        [DisplayName("FunctionalTest")]
        public int FunctionalTestId { get; set; }

        [ForeignKey("FunctionalTestId")]
        public virtual FunctionalTest FunctionalTest { get; set; }
    }
}