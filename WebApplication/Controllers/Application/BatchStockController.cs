using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using WebApplication.Attributes;
using WebApplication.Models.Application;

namespace WebApplication.Controllers.Application
{
    [AuthorizePermissions(Resource = "Batch")]
    public class BatchStockController : BaseAdminController<BatchStock>
    {
        BatchManager objects = new BatchManager();


        [AuthorizePermissions(Resource = "Batch", Operation = "Write")]
        public override ActionResult Create(BatchStock model, int? id, HttpPostedFileBase file)
        {
            model.UserId = User.Identity.Name;
            model.Date = DateTime.Now;
            db.Set<BatchStock>().Add(model);
            db.SaveChanges();


            return RedirectToAction("Details", "BatchStock");
        }

        [AuthorizePermissions(Resource = "Batch", Operation = "Read")]
        public override ActionResult Index()
        {
            return View();
        }

        [AuthorizePermissions(Resource = "Batch", Operation = "Read")]
        public ActionResult Details()
        {
            string nRequest = null;                     
            return View(objects.BatchStockGetAll(nRequest));

        }

        [AuthorizePermissions(Resource = "Batch", Operation = "Read")]
        [HttpPost]

        public ActionResult Details(BatchStock model, string nRequest, HttpPostedFileBase file)
        {
            var action = Request.Form.Get("Action");
           
            if (action == "Search")
            {
                if(nRequest == null || nRequest == "")
                {
                    ViewBag.Message = "Nº Request is empty!";

                }
                return View(objects.BatchStockGetAll(nRequest));

            }

            return View(objects.BatchStockGetAll(nRequest));

        }

    }
}
