using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using WebApplication.Models.Application;
using System.Data.Entity;
using WebApplication.Attributes;
using DataTables.AspNet.Mvc5;
using DataTables.AspNet.Core;

namespace WebApplication.Controllers.Application
{
    [AuthorizePermissions(Resource = "CosmeticStatus")]
    public class CosmeticStatusController : BaseAdminController<CosmeticStatus>
    {
        RequestManager rm = new RequestManager();

        [AuthorizePermissions(Resource = "CosmeticStatus", Operation = "Write")]
        public ActionResult Hold(int RequestId)
        {
            ViewBag.StatusList = PopulateDropDown(typeof(CosmeticStatus), "Name", "Name");
            ViewBag.RequestId = RequestId;
            ViewBag.CosmeticStatus = rm.GetStatusCosmetic(RequestId);
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthorizePermissions(Resource = "CosmeticStatus", Operation = "Write")]
        public ActionResult Hold(int RequestId, string Status, string OldStatus)
        {
            var request = db.RefurbRequests.Find(RequestId);

            request.StatusCosmetic = Status;
            db.Entry(request).State = EntityState.Modified;
            db.SaveChanges();

            if (OldStatus == "" || OldStatus == null) rm.CreateFlow(request, User.Identity.Name, Status);
            else rm.CreateFlow(request.Id, User.Identity.Name, "Changed to: " + Status, RequestFlowStatus.SentToCosmeticHold);

            return RedirectToAction("ByStatus", new { Status = Status });
        }

        [AuthorizePermissions(Resource = "CosmeticStatus", Operation = "Write")]
        public ActionResult Remove(int RequestId)
        {
            ViewBag.RequestId = RequestId;
            var request = db.RefurbRequests.Find(RequestId);
            return View();
        }

        [HttpPost, ActionName("Remove")]
        [ValidateAntiForgeryToken]
        [AuthorizePermissions(Resource = "CosmeticStatus", Operation = "Write")]
        public ActionResult RemoveConfirmed(int RequestId)
        {
            var request = db.RefurbRequests.Find(RequestId);

            request.StatusCosmetic = null;
            db.Entry(request).State = EntityState.Modified;
            db.SaveChanges();

            rm.CreateFlow(request.Id, User.Identity.Name, "Removed from hold", RequestFlowStatus.SentToCosmetic);

            return Redirect("~");
        }

        [AuthorizePermissions(Resource = "CosmeticStatus", Operation = "Read")]
        public ActionResult ByStatus(string Status)
        {
            ViewBag.Menu = Status;
            ViewBag.StatusList = PopulateDropDown(typeof(CosmeticStatus), "Name", "Name");

            return View();


            #region deprecated - remove the code above
            //var results = rm.RequestsByCosmeticStatus(Status);
            //ViewBag.Menu = Status;

            //ViewBag.StatusList = PopulateDropDown(typeof(CosmeticStatus), "Name", "Name");

            //return View(results); 
            #endregion
        }

        [AuthorizePermissions(Resource = "CosmeticStatus", Operation = "Write")]
        public override ActionResult Create(int? id)
        {
            return base.Create(id);
        }

        [AuthorizePermissions(Resource = "CosmeticStatus", Operation = "Write")]
        public override ActionResult Create(CosmeticStatus model, int? id, HttpPostedFileBase file)
        {
            return base.Create(model, id, file);
        }

        [AuthorizePermissions(Resource = "CosmeticStatus", Operation = "Delete")]
        public override ActionResult Delete(int? id)
        {
            return base.Delete(id);
        }

        [AuthorizePermissions(Resource = "CosmeticStatus", Operation = "Delete")]
        public override ActionResult DeleteConfirmed(int id)
        {
            return base.DeleteConfirmed(id);
        }

        [AuthorizePermissions(Resource = "CosmeticStatus", Operation = "Read")]
        public JsonResult PageData(IDataTablesRequest request, string status)
        {
            var data = db.RefurbRequests.Where(rr => rr.StatusCosmetic != null);
            
            var filteredData = data;
            if(!String.IsNullOrEmpty(status))
            {
                filteredData = data.Where(rr => rr.StatusCosmetic == status);
            }


            // card https://trello.com/c/QZzvlT2Z
            // sorting features enabled
            var colSorting = request.Columns.FirstOrDefault(c => c.Sort != null);
            if(colSorting == null)
            {
                filteredData = filteredData.OrderBy(f => f.Id);
            }
            else
            {
                switch(colSorting.Field)
                {
                    case "serialNumber":
                        if (colSorting.Sort.Direction == SortDirection.Ascending)
                            filteredData = filteredData.OrderBy(f => f.SerialNumber);
                        else
                            filteredData = filteredData.OrderByDescending(f => f.SerialNumber);
                        break;
                    case "dateRequested":
                        if (colSorting.Sort.Direction == SortDirection.Ascending)
                            filteredData = filteredData.OrderBy(f => f.DateRequested);
                        else
                            filteredData = filteredData.OrderByDescending(f => f.DateRequested);
                        break;

                    default:
                        if (colSorting.Sort.Direction == SortDirection.Ascending)
                            filteredData = filteredData.OrderBy(f => f.Id);
                        else
                            filteredData = filteredData.OrderByDescending(f => f.Id);
                        break;
                }
            }
            // eo card https://trello.com/c/QZzvlT2Z

            var dataPage = filteredData
                .Select(d => new
                {
                    d.Id,
                    d.Model.PartNumber,
                    d.SerialNumber,
                    d.UserId,
                    d.DateRequested,
                    d.StatusCosmetic
                })
                //.OrderBy(d => d.Id)
                .Skip(request.Start)
                .Take(request.Length)
                .ToList();

            var response = DataTablesResponse.Create(request, data.Count(), filteredData.Count(), dataPage);

            return new DataTablesJsonResult(response);
        }
    }
}