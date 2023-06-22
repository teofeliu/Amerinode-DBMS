using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.Entity;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebApplication.Models.Application
{

    public enum RequestFlowStatus
    {
        [Description("Received")]
        Received,

        [Description("Trial Performed")]
        TrialPerformed,

        [Description("Sent to repair")]
        SentToRepair,
        Repaired,

        [Description("Sent to cosmetic")]
        SentToCosmetic,
        [Description("Cosmetic Performed")]
        CosmeticPerformed,

        [Description("Sent to scrap evaluation")]
        SentToScrapEvaluation,

        [Description("Sent to scrap")]
        SentToScrap,

        [Description("Sent to DOA")]
        SentToDOA,

        [Description("Sent to final inspection")]
        SentToFinalInspection,

        [Description("Final Inspection")]
        FinalInspection,

        [Description("Sent to cosmetic hold")]
        SentToCosmeticHold,

        [Description("Sent back to trial")]
        SentBackToTrial,

        [Description("Delivered")]
        Delivered,

        [Description("Sent to BGA scrap evaluation")]
        SentToBgaScrapEvaluation,

        [Description("Sent to BGA scrap")]
        SentToBgaScrap,

        /* TODO: 
         * !!!  P L E A S E !!!
         *  Always that create a new enumeration, remember to update all ChartsController queries
         *      to keep all charts updated
         * */
    }

    public class RequestFlow
    {

        public RequestFlow()
        {
            this.DateRequested = DateTime.Now;
        }

        public RequestFlow(int RequestId, string UserId, RequestFlowStatus Status = RequestFlowStatus.Received, string Description = "Request Created") : this()
        {
            this.RequestId = RequestId;
            this.UserId = UserId;
            this.Status = Status;
            this.Description = Description;
        }

        [Required]
        [DisplayName("Id")]
        public int Id { get; set; }

        [Required]
        [DisplayName("Request")]
        public int RequestId { get; set; }

        [ForeignKey("RequestId")]
        public virtual RefurbRequest Request { get; set; }

        [Required]
        [DataType(DataType.MultilineText)]
        [DisplayName("Description")]
        public string Description { get; set; }

        [Required]
        [DisplayName("Request Date")]
        [DataType("DateTime")]
        public DateTime DateRequested { get; set; }

        [Required]
        [DisplayName("User")]
        public string UserId { get; set; }

        [Required]
        [DisplayName("Status")]
        public RequestFlowStatus Status { get; set; }

        public string TimeOnStatus()
        {
            DataModel db = new DataModel();

            //Pega todos os RequestFlows que sucedem o atual
            var nexts = db.RequestFlows.Where(x => x.RequestId == RequestId).Where(x => x.Id > Id);

            //Se o atual for o último, ainda não foi finalizado; retorna null
            if (nexts.Count() == 0) return null;
            else
            {
                //Dos próximos, pega o que tiver o menor Id. Ou seja, o próximo imediato.
                var next = nexts.First(x => x.Id == nexts.Min(y => y.Id));
                //DateRequested do próximo é igual a data que o atual foi finalizado. A conta resulta no tempo
                //que o request ficou no status atual.
                return (next.DateRequested - DateRequested).ToString("d'd 'h'h 'm'm 's's'");
            }
        }
    }
}
