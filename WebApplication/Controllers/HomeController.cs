using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using WebApplication.Models.Application;

namespace WebApplication.Controllers
{
    public class HomeController : Controller
    {
        RequestManager rm = new RequestManager();
        BatchManager bm = new BatchManager();
        public ActionResult DashboardV1()
        {
            return View();
        }

        #region PartialViews Atts
        public PartialViewResult AttPendingReview()
        {
            var table = bm.PendingReviewBatches().ToList();
            ViewBag.pendingReview = table.Count();
            return PartialView("BatchesPendingReview", table.OrderByDescending(x => x.Date).Take(5).ToList());
        }
        public PartialViewResult AttConferred()
        {
            var table = bm.ConferredBatches().ToList();
            ViewBag.conferred = table.Count();
            return PartialView("BatchesConferred", table.OrderByDescending(x => x.Date).Take(5).ToList());
        }
        public PartialViewResult AttTested()
        {
            var table = bm.TestedBatches().ToList();
            ViewBag.tested = table.Count();
            return PartialView("BatchesTested", table.OrderByDescending(x => x.Date).Take(5).ToList());
        }
        public PartialViewResult AttDivergent()
        {
            var table = bm.DivergentBatchesDash().ToList();
            ViewBag.divergences = table.Count();
            return PartialView("BatchesDivergent", table.OrderByDescending(x => x.Date).Take(5).ToList());
        }
        public PartialViewResult AttTrial()
        {
            var table = rm.ReceivedRequests().ToList();
            ViewBag.trial = table.Count();
            return PartialView("WaitingTrial", table.OrderByDescending(x => x.DateRequested).Take(5).ToList());
        }
        public PartialViewResult AttRepair()
        {
            var table = rm.SentToRefurbRequests().ToList();
            ViewBag.repair = table.Count();
            return PartialView("WaitingRepair", table.OrderByDescending(x => x.DateRequested).Take(5).ToList());
        }
        public PartialViewResult AttCosmetic()
        {
            var table = rm.SentToCosmeticRequests().ToList();
            ViewBag.cosmetic = table.Count();
            return PartialView("WaitingCosmetic", table.OrderByDescending(x => x.DateRequested).Take(5).ToList());
        }
        public PartialViewResult AttFinalInspection()
        {
            var table = rm.SentToExpedite().ToList();
            ViewBag.fi = table.Count();
            return PartialView("WaitingFinalInspection", table.OrderByDescending(x => x.DateRequested).Take(5).ToList());
        }
#endregion

        public ActionResult DashboardV2()
        {
            return View();
        }
    }
}