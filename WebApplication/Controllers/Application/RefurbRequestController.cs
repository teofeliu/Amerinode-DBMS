using System.Linq;
using System.Web;
using System.Web.Mvc;
using WebApplication.Models.Application;
using System.Collections.Generic;
using System;
using System.Linq.Dynamic;
using Rotativa;
using System.Reflection;
using Rotativa.Options;
using System.Data.Entity;
using WebApplication.Extensions;
using System.Text;
using DataTables.AspNet.Core;
using DataTables.AspNet.Mvc5;
using WebApplication.Attributes;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel;

namespace WebApplication.Controllers.Application
{
    [AuthorizePermissions(Resource = "RefurbRequest", Operation = "Read")]
    public class RefurbRequestController : BaseAdminController<RefurbRequest>
    {

        RequestManager objects = new RequestManager();

        [AuthorizePermissions(Resource = "RefurbRequest", Operation = "Read")]
        public override ActionResult Index()
        {
            ViewBag.StatusList = Enum.GetValues(typeof(RequestFlowStatus)).Cast<RequestFlowStatus>();
            ViewBag.OriginWarehouseList = db.Warehouses
                    .Where(w => w.WarehouseType == WarehouseType.Origin)
                    .ToList();
            ViewBag.DestinationWarehouseList = db.Warehouses
                    .Where(w => w.WarehouseType == WarehouseType.Destination)
                    .ToList();
            ViewBag.MinDate = db.RefurbRequests.Min(r => r.DateRequested).ToString("yyyyMMdd");
            
            return View();
        }

        [AuthorizePermissions(Resource = "RefurbRequest", Operation = "Read")]
        public JsonResult PageData(IDataTablesRequest request, RequestFlowStatus? status, AllRequestsFilter filters)
        {
            // card https://trello.com/c/15ZOC6Hz
            // do not restrict search to status
            IQueryable<RefurbRequest> data = db.RefurbRequests;
            if (String.IsNullOrEmpty(request.Search.Value))
            {
                data = data.Where(rr => !rr.Cancelled);
            }

            //if (String.IsNullOrEmpty(request.Search.Value))
            //{
            //    var statusNotAllowed = new[] { RequestFlowStatus.FinalInspection, RequestFlowStatus.SentToScrap };
            //    // statement implemented by card https://trello.com/c/7VW0MhUa
            //    if (status != null && status == RequestFlowStatus.SentToScrap)
            //    {
            //        statusNotAllowed = new[] { RequestFlowStatus.FinalInspection };
            //    }
            //    // eo statement implemented by card https://trello.com/c/7VW0MhUa

            //    data = db.RefurbRequests.Where(rr => !statusNotAllowed.Contains(rr.Status));
            //}
            //else {
            //    data = data.Where(rr => !rr.Cancelled);
            //}

            // eo card https://trello.com/c/15ZOC6Hz

            if (status != null)
            {
                data = data.Where(rr => rr.Status == status);
            }
            var filteredData = data;

            var attrs = GetFilterFields();
            if (!String.IsNullOrEmpty(request.Search.Value))
            {
                List<string> where = new List<string>();
                object[] param = new object[attrs.Count];
                Type t = new RefurbRequest().GetType();

                for (var i = 0; i < attrs.Count; i++)
                {
                    PropertyInfo prop = t.GetProperty(attrs[i]);

                    if (prop.PropertyType == typeof(string))
                    {
                        where.Add(attrs[i] + ".Contains(@" + i + ")");
                        param[i] = request.Search.Value;
                    }
                    if (prop.PropertyType == typeof(int) || prop.PropertyType == typeof(int?))
                    {
                        int val;
                        if (int.TryParse(request.Search.Value, out val))
                        {
                            where.Add(attrs[i] + " = @" + i);
                            param[i] = val;
                        }
                    }
                    if (prop.PropertyType == typeof(RequestFlowStatus))
                    {
                        RequestFlowStatus val;
                        if (Enum.TryParse<RequestFlowStatus>(request.Search.Value, true, out val))
                        {
                            where.Add(attrs[i] + " = @" + i + "");
                            param[i] = (RequestFlowStatus)val;
                        }
                    }
                }

                filteredData = filteredData.Where(x => !x.Cancelled).Where(String.Join(" || ", where), param);
            }

            #region card https://trello.com/c/hnsaxDtF
            // apply filters from all requests page

            if (filters.UseRequested)
            {
                var dateRequestedStart = DateTime.ParseExact(filters.DateRequestedStart, "yyyyMMdd", null);
                var dateRequestedEnd = DateTime.ParseExact(filters.DateRequestedEnd, "yyyyMMdd", null);
                filteredData = filteredData.Where(x => DbFunctions.TruncateTime(x.DateRequested) >= dateRequestedStart && DbFunctions.TruncateTime(x.DateRequested) <= dateRequestedEnd);
            }

            if (filters.UseDelivery)
            {
                var dateDeliveryStart = DateTime.ParseExact(filters.DateDeliveryStart, "yyyyMMdd", null);
                var dateDeliveryEnd = DateTime.ParseExact(filters.DateDeliveryEnd, "yyyyMMdd", null);
                // filteredData = filteredData.Where(x => x.DateDestination >= dateDeliveryStart && x.DateDestination <= dateDeliveryEnd);

                // this query is needed by mantain the same results to export query
                //      which is based on DateRequested in RequestFlow table
                var substatus = new[] { RequestFlowStatus.SentToScrap, RequestFlowStatus.SentToBgaScrap, RequestFlowStatus.Delivered };
                //var sub1 = db.RequestFlows.Where(rf =>
                //    substatus.Contains(rf.Status)
                //        && (rf.DateRequested >= dateDeliveryStart
                //        && rf.DateRequested <= dateDeliveryEnd))
                //.Select(rf => rf.RequestId);

                filteredData = filteredData.Where(x => db.RequestFlows.Any(rf =>
                    substatus.Contains(rf.Status)
                        && (DbFunctions.TruncateTime(rf.DateRequested) >= dateDeliveryStart
                        && DbFunctions.TruncateTime(rf.DateRequested) <= dateDeliveryEnd
                        && x.Id == rf.RequestId)));
            }

            if (filters.UseStatus)
            {
                filteredData = filteredData.Where(x => filters.Status.Contains(x.Status));
            }

            if (filters.UseOrigin)
            {
                filteredData = filteredData.Where(x => filters.Origin.Contains(x.OriginId));
            }

            if (filters.UseDestination)
            {
                filteredData = filteredData.Where(x => filters.Destination.Contains(x.DestinationId));
            }
            #endregion

            // card https://trello.com/c/QZzvlT2Z
            // sorting features enabled


            var colSorting = request.Columns.FirstOrDefault(c => c.Sort != null);
            if (colSorting == null)
            {
                filteredData = filteredData.OrderBy(f => f.Id);
            }
            else
            {
                switch (colSorting.Field)
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
                    case "lastUpdated":
                        if (colSorting.Sort.Direction == SortDirection.Ascending)
                            filteredData = filteredData.OrderBy(f => f.LastUpdated);
                        else
                            filteredData = filteredData.OrderByDescending(f => f.LastUpdated);
                        break;
                    default:
                        if (colSorting.Sort.Direction == SortDirection.Ascending)
                            filteredData = filteredData.OrderBy(f => f.Id);
                        else
                            filteredData = filteredData.OrderByDescending(f => f.Id);
                        break;
                }
            }
            // eo // card https://trello.com/c/QZzvlT2Z



            var dataPage = filteredData
                .Select(d => new
                {
                    d.Id,
                    d.Model.PartNumber,
                    d.SerialNumber,
                    d.UserId,
                    d.DateRequested,
                    d.LastUpdated,
                    d.Status
                })
                //.OrderBy(d => d.Id)
                .Skip(request.Start)
                .Take(request.Length)
                .ToList()
                .Select(d => new
                {
                    d.Id,
                    d.PartNumber,
                    d.SerialNumber,
                    d.UserId,
                    d.DateRequested,
                    d.LastUpdated,
                    statusDescription = Utility.GetDescriptionFromEnumValue(d.Status),
                    supply = objects.GetSupplyDescription(d.Id),
                    inWarranty = objects.GetWarrantyVerbose(d.Id),
                    d.Status
                });

            var response = DataTablesResponse.Create(request, data.Count(), filteredData.Count(), dataPage);

            return new DataTablesJsonResult(response);
        }

        [AuthorizePermissions(Resource = "RefurbRequest", Operation = "Write")]
        public override ActionResult Create(int? id)
        {
            #region card https://trello.com/c/gErIEnsQ
            // feature disabled by implementation of card https://trello.com/c/gErIEnsQ
            throw new InvalidOperationException("This feature is disabled. You cannot call the URL directly.");
            #endregion


            object selected = null;
            object selectedBatch = null;
            var model = new RefurbRequest(User.Identity.Name);
            if (id != null)
            {
                model = objects.GetRequest((int)id);
                selected = model.ModelId;
            }
            ViewBag.Models = PopulateDropDown(typeof(Model), "Id", "Name", selected);

            // do not allowed to list all batches, just list batches that has conffered
            //ViewBag.Batches = PopulateDropDown(typeof(Batch), "Id", "Id", selectedBatch);
            var allowed = new[] { BatchStatus.Conferred };
            var batches = db.Batches.Where(b => allowed.Contains(b.Status)).ToList();
            ViewBag.Batches = new SelectList(batches, "Id", "Id", selectedBatch);


            ViewBag.HardwareOverviews = objects.HardwareOverviews();
            ViewBag.CosmeticOverviews = objects.CosmeticOverviews();
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthorizePermissions(Resource = "RefurbRequest", Operation = "Write")]
        public override ActionResult Create(RefurbRequest model, int? id, HttpPostedFileBase file)
        {
            #region card https://trello.com/c/gErIEnsQ
            // feature disabled by implementation of card https://trello.com/c/gErIEnsQ
            throw new InvalidOperationException("This feature is disabled. You cannot call the URL directly.");
            #endregion

            object selected = model.ModelId;
            object selectedBatch = model.BatchId;
            ViewBag.Models = PopulateDropDown(typeof(Model), "Id", "Name", selected);
            ViewBag.Batches = PopulateDropDown(typeof(Batch), "Id", "Id", selectedBatch);
            ViewBag.HardwareOverviews = objects.HardwareOverviews();
            ViewBag.CosmeticOverviews = objects.CosmeticOverviews();

            if (ModelState.IsValid)
            {

                string mc = Request.Form.Get("MissingChecklistItem");
                string cc = Request.Form.Get("CosmeticChecklistItem");

                if (mc == null && cc == null)
                {
                    ViewBag.Message = "Please, select at least 1 item from each checklist.";
                    return View(model);
                }

                try
                {
                    objects.ReceiveRequest(model, mc, cc);
                }
                catch (Exception ex)
                {
                    ViewBag.Message = (ex.InnerException != null)
                        ? ex.InnerException.Message
                        : ex.Message;
                    return View(model);
                }

                return RedirectToAction("Index");
            }

            var errors = ModelState.Select(x => x.Value.Errors)
                           .Where(y => y.Count > 0)
                           .ToList();
            ViewBag.Message = "Something is wrong with yor data";
            return View(model);
        }


        [AuthorizePermissions(Resource = "RefurbRequest", Operation = "Read")]
        public ActionResult Trial(int RequestId)
        {
            ViewBag.Request = objects.GetRequest(RequestId);
            ViewBag.CosmeticOverviews = objects.CosmeticOverview();
            ViewBag.Components = objects.FunctionalTests();

            //ViewBag.Overviews = PopulateDropDown(typeof(CosmeticOverview), "Id", "Name");

            TrialViewModel model = new TrialViewModel(User.Identity.Name, RequestId);
            model.InWarranty = db.Warranties.Any(w => w.RequestId == RequestId);

            
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthorizePermissions(Resource = "RefurbRequest", Operation = "Write")]
        public ActionResult Trial(TrialViewModel model, int RequestId, HttpPostedFileBase file)
        {
            ViewBag.Request = objects.GetRequest(RequestId);
            ViewBag.Components = objects.FunctionalTests();
            ViewBag.CosmeticOverviews = objects.CosmeticOverview();

            //object selected = model.OverviewId;
            //ViewBag.Overviews = PopulateDropDown(typeof(CosmeticOverview), "Id", "Name", selected
            //    );

            var warrantyVerbose = objects.GetWarrantyVerbose(RequestId);

            if ((model.InWarranty && warrantyVerbose == "To be confirmed")
                ||
                (!model.InWarranty && !(warrantyVerbose == "To be confirmed"))) ModelState.Remove("WarrantyDescription");
            if (ModelState.IsValid)
            {
                var status = RequestFlowStatus.SentToCosmetic;
                string ft = Request.Form.Get("FunctionalTests");
                string ov = Request.Form.Get("Overview");
                var action = Request.Form.Get("Action");

                if (action == "Send to Repair")
                {
                    status = RequestFlowStatus.SentToRepair;
                    if (ft == null)
                    {
                        ViewBag.Message = "Please, select at least 1 functional test to continue with sent to repair.";
                        if (warrantyVerbose == "To be confirmed") model.InWarranty = true;
                        return View(model);
                    }
                }else
                {
                    if (ft != null)
                    {
                        ViewBag.Message = "Please, do not select items if you want to proceed to cosmetic.";
                        if (warrantyVerbose == "To be confirmed") model.InWarranty = true;
                        return View(model);
                    }

                    if (action == "Send to Cosmetic" && ov == null)
                    {
                        ViewBag.Message = "Please, select at least 1 cosmetic Overview to continue with sent to Cosmetic.";
                        return View(model);
                    }

                    if (action == "Send to Scrap Evaluation")
                        status = RequestFlowStatus.SentToScrapEvaluation;

                    else if (action == "Send to BGA Scrap Evaluation")
                        status = RequestFlowStatus.SentToBgaScrapEvaluation;

                    else if (action == "Send To DOA")
                        status = RequestFlowStatus.SentToDOA;
                   
                        
                }


                var trial = new Trial
                {
                    RequestId = model.RequestId,
                    Description = model.Description,
                    Date = model.Date,
                    UserId = model.UserId
                };
                objects.PerformTrial(trial, status, ft);
                objects.UpdateWarranty(model.RequestId, model.InWarranty, model.WarrantyDescription);
                objects.CosmeticChecklists(model.RequestId, ov);

                return RedirectToAction("Index");
            }
            ViewBag.Message = "Something is wrong with yor data.\n" + ViewBag.Message;
            if (warrantyVerbose == "To be confirmed") model.InWarranty = true;
            return View(model);
        }


        [AuthorizePermissions(Resource = "RefurbRequest", Operation = "Read")]
        public ActionResult Cosmetic(int RequestId)
        {
            ViewBag.Request = objects.GetRequest(RequestId);
            Cosmetic model = new Cosmetic(User.Identity.Name, RequestId);
            ViewBag.Supplies = PopulateDropDown(typeof(Supply), "Id", "Name");
            
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthorizePermissions(Resource = "RefurbRequest", Operation = "Write")]
        public ActionResult Cosmetic(Cosmetic model, int RequestId, HttpPostedFileBase file)
        {
            ViewBag.Request = objects.GetRequest(RequestId);
            object selected = model.SupplyId;

            var action = Request.Form.Get("Action");
            if (action == "Send back to Trial")
            {

                ModelState.Remove("Supply");
                if (ModelState.IsValid)
                {
                    objects.BackToTrial(ViewBag.Request, User.Identity.Name, model.Description);
                    return RedirectToAction("Index");
                }
            }
            else
            {
                if (db.Cosmetics.Any(c => c.RequestId == RequestId))
                {
                    if (db.FinalInspections.Any(fi => fi.RequestId == RequestId)) ViewBag.Message = "This Request already went through this process.";
                    else ViewBag.Message = "This Request is already flagged as Final Inspection. You can take action <a href='/RefurbRequest/FinalInspection?RequestId=" + RequestId + "'>here</a>.";
                    ViewBag.Supplies = PopulateDropDown(typeof(Supply), "Id", "Name", selected);
                    return View(model);
                }
                if (ModelState.IsValid)
                {
                    objects.PerformCosmetic(model);
                    return RedirectToAction("Index");
                }
            }

            ViewBag.Supplies = PopulateDropDown(typeof(Supply), "Id", "Name", selected);
            ViewBag.Message = "Something is wrong with yor data";
            return View(model);
        }


        [AuthorizePermissions(Resource = "RefurbRequest", Operation = "Read")]
        public ActionResult Repair(int RequestId)
        {
            ViewBag.Request = objects.GetRequest(RequestId);
            ViewBag.Components = objects
                .RepairTypes()
                .Select(rt => new SelectListItem {
                    Value = rt.Id.ToString(),
                    Text = rt.Name
                })
                .ToList();

            Repair model = new Repair(User.Identity.Name, RequestId);
            
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthorizePermissions(Resource = "RefurbRequest", Operation = "Write")]
        public ActionResult Repair(Repair model, int RequestId, HttpPostedFileBase file)
        {
            ViewBag.Request = objects.GetRequest(RequestId);
            ViewBag.Components = objects.RepairTypes();
            if (ModelState.IsValid)
            {
                var status = RequestFlowStatus.SentToCosmetic;
                string tc = Request.Form.Get("TrialRepairTypes");
                var action = Request.Form.Get("Action");
                if (action == "Send to Cosmetic")
                {
                    if (tc == null)
                    {
                        ViewBag.Message = "Please, select at least 1 teste failed to continue with sent to repair.";
                        return View(model);
                    }

                }
                else
                {
                    status = RequestFlowStatus.SentToScrapEvaluation;

                    if (action.Equals("Send to BGA Scrap Evaluation"))
                    {
                        status = RequestFlowStatus.SentToBgaScrapEvaluation;
                    }
                }
                objects.PerformRepair(model, status, tc);

                return RedirectToAction("Index");
            }
            ViewBag.Message = "Something is wrong with yor data";
            return View(model);
        }

        [AuthorizePermissions(Resource = "RefurbRequest", Operation = "Read")]
        public ActionResult Scrap(int RequestId)
        {
            throw new InvalidOperationException("This feature is disabled. " +
                "You need to create a batch to process the scraps. Do not call the URL directly.");
            ViewBag.Request = objects.GetRequest(RequestId);
            ViewBag.Components = objects.RepairTypes();
            Scrap model = new Scrap(User.Identity.Name, RequestId);
            
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthorizePermissions(Resource = "RefurbRequest", Operation = "Write")]
        public ActionResult Scrap(Scrap model, int RequestId, HttpPostedFileBase file)
        {
            throw new InvalidOperationException("This feature is disabled. " +
                "You need to create a batch to process the scraps. Do not call the URL directly.");


            ViewBag.Request = objects.GetRequest(RequestId);
            if (ModelState.IsValid)
            {
                objects.PerformScrap(model);
                return RedirectToAction("Index");
            }
            ViewBag.Message = "Something is wrong with yor data";
            return View(model);
        }

        [AuthorizePermissions(Resource = "RefurbRequest", Operation = "Read")]
        public ActionResult FinalInspection(int RequestId)
        {
            ViewBag.Request = objects.GetRequest(RequestId);
            FinalInspection model = new FinalInspection(User.Identity.Name, RequestId);
            

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthorizePermissions(Resource = "RefurbRequest", Operation = "Write")]
        public ActionResult FinalInspection(FinalInspection model, int RequestId, HttpPostedFileBase file)
        {
            var action = Request.Form.Get("Action");

            ViewBag.Request = objects.GetRequest(RequestId);
            var requestId = objects.GetRequest(RequestId);

            if (ModelState.IsValid)
            {
                if (action == "Send back to Cosmetic")
                {
                    var status = RequestFlowStatus.SentToCosmetic;
                    objects.UpdateStatus(model, status);
                    //return RedirectToAction("Cosmetic",RequestId);                    
                    //return RedirectToAction("Cosmetic", "RefurbRequest",requestId);
                    return RedirectToAction("Index");


                }
                else if (action == "Send back to Repair")
                {
                    var status = RequestFlowStatus.SentToRepair;
                    objects.UpdateStatus(model, status);
                    //return RedirectToAction("Repair", requestId);
                    return RedirectToAction("Index");


                }
                else if (action == "Send back to Trial")
                {
                    var status = RequestFlowStatus.SentBackToTrial;
                    objects.UpdateStatus(model, status);
                    //return RedirectToAction("Repair", requestId);
                    return RedirectToAction("Index");


                }
                else
                {
                    objects.PerformFinalInspection(model);
                    return RedirectToAction("Index");
                }
      
            }
            ViewBag.Message = "Something is wrong with yor data";
            return View(model);
        }


        [AuthorizePermissions(Resource = "RefurbRequest", Operation = "Read")]
        public ActionResult ByStatus(RequestFlowStatus Status = RequestFlowStatus.Received, string q = "")
        {
            ViewBag.Status = Status;

            switch (Status)
            {
                case RequestFlowStatus.Received:
                    ViewBag.Menu = "trial";
                    break;
                case RequestFlowStatus.SentToRepair:
                    ViewBag.Menu = "repair";
                    break;
                case RequestFlowStatus.SentToCosmetic:
                    ViewBag.Menu = "cosmetic";
                    break;
                case RequestFlowStatus.SentToFinalInspection:
                    ViewBag.Menu = "final-inspection";
                    break;
                case RequestFlowStatus.FinalInspection:
                    ViewBag.Menu = "delivered";
                    break;
                case RequestFlowStatus.Delivered:
                    ViewBag.Menu = "end-of-cycle";
                    break;
                case RequestFlowStatus.SentToScrap:
                case RequestFlowStatus.SentToBgaScrap:
                    ViewBag.Menu = "end-of-cycle-scrap";
                    break;
                case RequestFlowStatus.Repaired:
                case RequestFlowStatus.TrialPerformed:
                case RequestFlowStatus.CosmeticPerformed:
                case RequestFlowStatus.SentToScrapEvaluation:
                case RequestFlowStatus.SentToBgaScrapEvaluation:
                case RequestFlowStatus.SentToDOA:
                case RequestFlowStatus.SentToCosmeticHold:
                case RequestFlowStatus.SentBackToTrial:
                default:
                    ViewBag.Menu = "requests";
                    break;
            }
            
            
return View();
        }

        [AuthorizePermissions(Resource = "RefurbRequest", Operation = "Read")]
        public ActionResult IndexAsPdf()
        {
            var statusNotAllowed = new[] { RequestFlowStatus.FinalInspection, RequestFlowStatus.SentToScrap, RequestFlowStatus.SentToBgaScrap };
            // statement implemented by card https://trello.com/c/7VW0MhUa
            var status = Request.QueryString["status"];
            if (!String.IsNullOrEmpty(status))
            {
                RequestFlowStatus val;
                if (Enum.TryParse<RequestFlowStatus>(status, out val))
                {
                    statusNotAllowed = new[] { RequestFlowStatus.FinalInspection };
                }
            }
            // eo statement implemented by card https://trello.com/c/7VW0MhUa

            var attrs = GetFilterFields();
            if (Request.QueryString.Count > 0)
            {
                String q = Request.QueryString["q"];

                List<string> where = new List<string>();
                object[] param = new object[attrs.Count];

                Type t = new RefurbRequest().GetType();

                for (var i = 0; i < attrs.Count; i++)
                {
                    PropertyInfo prop = t.GetProperty(attrs[i]);

                    if (prop.PropertyType == typeof(string) && !String.IsNullOrEmpty(q))
                    {
                        where.Add(attrs[i] + ".Contains(@" + i + ")");
                        param[i] = q;
                    }
                    if ((prop.PropertyType == typeof(int) || prop.PropertyType == typeof(int?))
                            && !String.IsNullOrEmpty(q))
                    {
                        int val;
                        if (int.TryParse(q, out val))
                        {
                            where.Add(attrs[i] + "=@" + i);
                            param[i] = val;
                        }
                    }
                    if (prop.PropertyType == typeof(RequestFlowStatus)
                        && !String.IsNullOrEmpty(status))
                    {
                        RequestFlowStatus val;
                        if (Enum.TryParse<RequestFlowStatus>(
                            status, true, out val))
                        {
                            where.Add(attrs[i] + " = @" + i + "");
                            param[i] = (RequestFlowStatus)val;
                        }
                    }
                }
                List<RefurbRequest> results = db.Set<RefurbRequest>()
                    .AsQueryable()
                    .Where(String.Join(" || ", where), param)
                    .ToList<RefurbRequest>();

                // statement implemented by card https://trello.com/c/7VW0MhUa
                if (!String.IsNullOrEmpty(status))
                {
                    RequestFlowStatus val;
                    if (Enum.TryParse<RequestFlowStatus>(status, true, out val))
                    {
                        ViewBag.Status = Utility.GetDescriptionFromEnumValue(val);
                    }
                }
                // eo statement implemented by card https://trello.com/c/7VW0MhUa

                return new ViewAsPdf
                {
                    Model = results,
                    FileName = String.IsNullOrEmpty(status)
                        ? string.Format("all-requests.{0:yyyyMMddHHmm}.pdf", DateTime.Now)
                        : string.Format("all-requests-{1}.{0:yyyyMMddHHmm}.pdf", DateTime.Now, status),
                    PageSize = Size.A4
                };
            }

            var result = db.RefurbRequests
                .Where(r => !statusNotAllowed.Contains(r.Status))
                .ToList();

            return new ViewAsPdf
            {
                Model = result,
                FileName = String.IsNullOrEmpty(status)
                        ? string.Format("all-requests.{0:yyyyMMddHHmm}.pdf", DateTime.Now)
                        : string.Format("all-requests-{1}.{0:yyyyMMddHHmm}.pdf", DateTime.Now, status),
                PageSize = Size.A4
            };
        }

        [AuthorizePermissions(Resource = "RefurbRequest", Operation = "Read")]
        public CsvActionResult<RefurRequestExportCsvModel> IndexAsCsv()
        {
            var statusNotAllowed = new[] { RequestFlowStatus.FinalInspection, RequestFlowStatus.SentToScrap, RequestFlowStatus.SentToBgaScrap };
            // statement implemented by card https://trello.com/c/7VW0MhUa
            var status = Request.QueryString["status"];
            if (!String.IsNullOrEmpty(status))
            {
                RequestFlowStatus val;
                if (Enum.TryParse<RequestFlowStatus>(status, out val))
                {
                    statusNotAllowed = new[] { RequestFlowStatus.FinalInspection };
                }
            }
            // eo statement implemented by card https://trello.com/c/7VW0MhUa

            var attrs = GetFilterFields();
            if (Request.QueryString.Count > 0)
            {
                String q = Request.QueryString["q"];

                List<string> where = new List<string>();
                object[] param = new object[attrs.Count];

                Type t = new RefurbRequest().GetType();

                for (var i = 0; i < attrs.Count; i++)
                {
                    PropertyInfo prop = t.GetProperty(attrs[i]);

                    if (prop.PropertyType == typeof(string) && !String.IsNullOrEmpty(q))
                    {
                        where.Add(attrs[i] + ".Contains(@" + i + ")");
                        param[i] = q;
                    }
                    if ((prop.PropertyType == typeof(int) || prop.PropertyType == typeof(int?))
                            && !String.IsNullOrEmpty(q))
                    {
                        int val;
                        if (int.TryParse(q, out val))
                        {
                            where.Add(attrs[i] + "=@" + i);
                            param[i] = val;
                        }
                    }
                    if (prop.PropertyType == typeof(RequestFlowStatus)
                        && !String.IsNullOrEmpty(status))
                    {
                        RequestFlowStatus val;
                        if (Enum.TryParse<RequestFlowStatus>(
                            status, true, out val))
                        {
                            where.Add(attrs[i] + " = @" + i + "");
                            param[i] = (RequestFlowStatus)val;
                        }
                    }
                }
                var filteredResults = db.Set<RefurbRequest>()
                    .AsQueryable()
                    .Where(String.Join(" || ", where), param)
                    .Select(rr => new RefurRequestExportCsvModel
                    {
                        TM = rr.Model.TM,
                        ModelName = rr.Model.Name,
                        ModelTypeName = rr.Model.Type.Name,
                        SerialNumber = rr.SerialNumber
                    })
                    .ToList();

                // statement implemented by card https://trello.com/c/7VW0MhUa
                if (!String.IsNullOrEmpty(status))
                {
                    RequestFlowStatus val;
                    if (Enum.TryParse<RequestFlowStatus>(status, true, out val))
                    {
                        ViewBag.Status = Utility.GetDescriptionFromEnumValue(val);
                    }
                }
                // eo statement implemented by card https://trello.com/c/7VW0MhUa


                return new CsvActionResult<RefurRequestExportCsvModel>(filteredResults,
                    String.IsNullOrEmpty(status)
                        ? string.Format("all-requests.{0:yyyyMMddHHmm}.csv", DateTime.Now)
                        : string.Format("all-requests-{1}.{0:yyyyMMddHHmm}.csv", DateTime.Now, status));

            }

            var results = db.RefurbRequests
                .Where(r => !statusNotAllowed.Contains(r.Status))
                .Select(rr => new RefurRequestExportCsvModel
                {
                    TM = rr.Model.TM,
                    ModelName = rr.Model.Name,
                    ModelTypeName = rr.Model.Type.Name,
                    SerialNumber = rr.SerialNumber
                })
                .ToList();


            return new CsvActionResult<RefurRequestExportCsvModel>(results,
                    String.IsNullOrEmpty(status)
                        ? string.Format("all-requests.{0:yyyyMMddHHmm}.csv", DateTime.Now)
                        : string.Format("all-requests-{1}.{0:yyyyMMddHHmm}.csv", DateTime.Now, status));
        }

        [AuthorizePermissions(Resource = "RefurbRequest", Operation = "Read")]
        public ActionResult Details(int id)
        {
            ViewBag.Request = objects.GetRequest(id);
            ViewBag.Warranty = objects.GetRequestWarranty(id);
            return View();
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [AuthorizePermissions(Resource = "RefurbRequest", Operation = "Delete")]
        public override ActionResult DeleteConfirmed(int id)
        {
            var entity = db.RefurbRequests.Find(id);
            entity.Cancelled = true;
            db.Entry(entity).State = EntityState.Modified;
            db.SaveChanges();

            return RedirectToAction("Index");
        }

        [AuthorizePermissions(Resource = "RefurbRequest", Operation = "Read")]
        public ActionResult Delivery(int requestId)
        {
            throw new NotSupportedException("This action is disallowed. Do not call the endpoint manually, please.");
            ViewBag.Status = RequestFlowStatus.FinalInspection;
            ViewBag.Request = objects.GetRequest(requestId);
            Delivery model = new Delivery(User.Identity.Name, requestId);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthorizePermissions(Resource = "RefurbRequest", Operation = "Write")]
        public ActionResult Delivery(Delivery model, int requestId)
        {
            throw new NotSupportedException("This action is disallowed. Do not call the endpoint manually, please.");

            ViewBag.Status = RequestFlowStatus.FinalInspection;
            ViewBag.Request = objects.GetRequest(requestId);

            if (ModelState.IsValid)
            {
                objects.PerformDelivery(model);
                return RedirectToAction("ByStatus", new { status = ViewBag.Status });
            }

            ViewBag.Message = "Something is wrong with yor data";
            return View(model);
        }

        [AuthorizePermissions(Resource = "RefurbRequest", Operation = "Write")]
        public CsvActionResult<RefurbRequestExportModel> ExportAllAsCsv(AllRequestsFilter filters)
        {
            var query = new StringBuilder();
            query.Append(" WITH [StatusFlowPivot] ");
            query.Append(" AS ( ");
            query.Append(" 	SELECT [RequestId] ");
            query.Append(" 		, [0] AS [ReceivedAt] ");
            query.Append(" 		, [1] AS [TrialPerformedAt] ");
            query.Append(" 		, [2] AS [SentToRepairAt] ");
            query.Append(" 		, [3] AS [RepairedAt] ");
            query.Append(" 		, [4] AS [SentToCosmeticAt] ");
            query.Append(" 		, [5] AS [CosmeticPerformedAt] ");
            query.Append(" 		, [6] AS [SentToScrapEvaluationAt] ");
            query.Append(" 		, [14] AS [SentToBgaScrapEvaluationAt] ");
            query.Append(" 		, [7] AS [SentToScrapAt] ");
            query.Append(" 		, [15] AS [SentToBgaScrapAt] ");
            query.Append(" 		, [8] AS [SentToDOAAt] ");
            query.Append(" 		, [9] AS [SentToFinalInspectionAt] ");
            query.Append(" 		, [10] AS [FinalInspectionAt] ");
            query.Append(" 		, [11] AS [SentToCosmeticHoldAt] ");
            query.Append(" 		, [12] AS [SentBackToTrialAt] ");
            query.Append(" 		, [13] AS [Delivered] ");
            query.Append(" 	FROM ( ");
            query.Append(" 		SELECT [rf].* ");
            query.Append(" 		FROM [dbo].[RequestFlows] AS [rf] ");
            query.Append(" 		WHERE 1 = 1 ");
            query.Append(" 		) AS [src] ");
            query.Append(" 	PIVOT(MIN([DateRequested]) FOR [Status] IN ([0], [1], [2], [3], [4], [5], [6], [7], [8], [9], [10], [11], [12], [13], [14], [15])) AS [pivoted] ");
            query.Append(" 	) ");
            query.Append(" SELECT [rr].[Id] AS [RequestId] ");
            query.Append(" 	, [rr].[ModelId] ");
            query.Append(" 	, [m].[Name] AS [Model] ");
            query.Append(" 	, [m].[TM] AS [SAPCode] ");
            query.Append(" 	, [m].[PartNumber] ");
            query.Append(" 	, [m].[ModelTypeId] ");
            query.Append(" 	, [mt].[Name] AS [ModelType] ");
            query.Append(" 	, [m].[ManufacturerId] ");
            query.Append(" 	, [ma].[Name] AS [Manufacturer] ");
            query.Append(" 	, [rr].[SerialNumber] ");
            query.Append(" 	, [rr].[DateRequested] ");
            query.Append(" 	, [rr].[IsDOA] ");
            query.Append(" 	, [rr].[Invoice] ");
            query.Append(" 	, CASE [rr].[Status] ");
            query.Append(" 		WHEN 0 ");
            query.Append(" 			THEN 'Received' ");
            query.Append(" 		WHEN 1 ");
            query.Append(" 			THEN 'TrialPerformed' ");
            query.Append(" 		WHEN 2 ");
            query.Append(" 			THEN 'SentToRepair' ");
            query.Append(" 		WHEN 3 ");
            query.Append(" 			THEN 'Repaired' ");
            query.Append(" 		WHEN 4 ");
            query.Append(" 			THEN 'SentToCosmetic' ");
            query.Append(" 		WHEN 5 ");
            query.Append(" 			THEN 'CosmeticPerformed' ");
            query.Append(" 		WHEN 6 ");
            query.Append(" 			THEN 'SentToScrapEvaluation' ");
            query.Append(" 		WHEN 7 ");
            query.Append(" 			THEN 'SentToScrap' ");
            query.Append(" 		WHEN 8 ");
            query.Append(" 			THEN 'SentToDOA' ");
            query.Append(" 		WHEN 9 ");
            query.Append(" 			THEN 'SentToFinalInspection' ");
            query.Append(" 		WHEN 10 ");
            query.Append(" 			THEN 'FinalInspection' ");
            query.Append(" 		WHEN 11 ");
            query.Append(" 			THEN 'SentToCosmeticHold' ");
            query.Append(" 		WHEN 12 ");
            query.Append(" 			THEN 'SentBackToTrial' ");
            query.Append(" 		WHEN 13 ");
            query.Append(" 			THEN 'Delivered' ");
            query.Append(" 		WHEN 14 ");
            query.Append(" 			THEN 'SentToBgaScrapEvaluation' ");
            query.Append(" 		WHEN 15 ");
            query.Append(" 			THEN 'SentToBgaScrap' ");
            query.Append(" 		ELSE 'N/A' ");
            query.Append(" 		END AS [Status] ");
            query.Append(" 	, ISNULL(CASE [w].[InWarranty] ");
            query.Append(" 			WHEN 0 ");
            query.Append(" 				THEN 'Denied' ");
            query.Append(" 			WHEN 1 ");
            query.Append(" 				THEN ( ");
            query.Append(" 						CASE [rr].[Status] ");
            query.Append(" 							WHEN 0 ");
            query.Append(" 								THEN 'To be confirmed' ");
            query.Append(" 							ELSE 'Yes' ");
            query.Append(" 							END ");
            query.Append(" 						) ");
            query.Append(" 			END, 'No') AS [InWarranty] ");
            query.Append(" 	, [rr].[BatchId] ");
            query.Append(" 	, MIN([pvt].[ReceivedAt]) AS [ReceivedAt] ");
            query.Append(" 	, [wo].[Id] + ' - ' + [wo].[Name] AS [Origin] ");
            query.Append(" 	, MIN([pvt].[TrialPerformedAt]) AS [TrialPerformedAt] ");
            query.Append(" 	, [ww].[Id] + ' - ' + [ww].[Name] AS [Warehouse] ");
            query.Append(" 	, [rr].[DateWarehouse] ");
            query.Append(" 	, MIN([pvt].[SentToRepairAt]) AS [SentToRepairAt] ");
            query.Append(" 	, MIN([pvt].[RepairedAt]) AS [RepairedAt] ");
            query.Append(" 	, [RepairDescription] = STUFF(( ");
            query.Append(" 			SELECT '; ' + [Description] ");
            query.Append(" 			FROM [dbo].[Repairs] ");
            query.Append(" 			WHERE [RequestId] = [rr].[Id] ");
            query.Append(" 			FOR XML PATH('') ");
            query.Append(" 				, type ");
            query.Append(" 			).value('.', 'nvarchar(max)'), 1, 2, '') ");
            query.Append(" 	, [RepairChecklist] = STUFF(( ");
            query.Append(" 			SELECT '; ' + [Name] ");
            query.Append(" 			FROM [dbo].[Repairs] AS [re] ");
            query.Append(" 			INNER JOIN [dbo].[RepairRepairTypes] AS [rert] ON [rert].[RepairId] = [re].[Id] ");
            query.Append(" 			INNER JOIN [dbo].[RepairTypes] AS [ret] ON [ret].[Id] = [rert].[RepairTypeId] ");
            query.Append(" 			WHERE [RequestId] = [rr].[Id] ");
            query.Append(" 			FOR XML PATH('') ");
            query.Append(" 				, type ");
            query.Append(" 			).value('.', 'nvarchar(max)'), 1, 2, '') ");
            query.Append(" 	, [Supply] = ( ");
            query.Append(" 		SELECT DISTINCT [as].[Description] ");
            query.Append(" 		FROM [dbo].[Supplies] AS [as] ");
            query.Append(" 		INNER JOIN [dbo].[Cosmetics] AS [ac] ON ( ");
            query.Append(" 				[as].[Id] = [ac].[SupplyId] ");
            query.Append(" 				AND [ac].[RequestId] = [rr].[Id] ");
            query.Append(" 				) ");
            query.Append(" 		) ");
            query.Append(" 	, MIN([pvt].[SentToCosmeticAt]) AS [SentToCosmeticAt] ");
            query.Append(" 	, MIN([pvt].[CosmeticPerformedAt]) AS [CosmeticPerformedAt] ");
            query.Append(" 	, MIN([pvt].[SentToScrapEvaluationAt]) AS [SentToScrapEvaluationAt] ");
            query.Append(" 	, MIN([pvt].[SentToScrapAt]) AS [SentToScrapAt] ");
            query.Append(" 	, MIN([pvt].[SentToDOAAt]) AS [SentToDOAAt] ");
            query.Append(" 	, MIN([pvt].[SentToFinalInspectionAt]) AS [SentToFinalInspectionAt] ");
            query.Append(" 	, MIN([pvt].[FinalInspectionAt]) AS [FinalInspectionAt] ");
            query.Append(" 	, MIN([pvt].[SentToCosmeticHoldAt]) AS [SentToCosmeticHoldAt] ");
            query.Append(" 	, MIN([pvt].[SentBackToTrialAt]) AS [SentBackToTrialAt] ");
            query.Append(" 	, MIN([pvt].[SentToBgaScrapEvaluationAt]) AS [SentToBgaScrapEvaluationAt] ");
            query.Append(" 	, MIN([pvt].[SentToBgaScrapAt]) AS [SentToBgaScrapAt] ");
            query.Append(" 	, MIN(COALESCE([pvt].[Delivered], [pvt].[SentToBgaScrapAt], [pvt].[SentToScrapAt])) AS [Delivered] ");
            query.Append(" 	, [wd].[Id] + ' - ' + [wd].[Name] AS [Destination] ");
            query.Append(" FROM [dbo].[RefurbRequests] AS [rr] ");
            query.Append(" INNER JOIN [dbo].[Models] AS [m] ON [rr].[ModelId] = [m].[Id] ");
            query.Append(" INNER JOIN [dbo].[ModelTypes] AS [mt] ON [m].[ModelTypeId] = [mt].[Id] ");
            query.Append(" LEFT JOIN [dbo].[Manufacturers] AS [ma] ON [m].[ManufacturerId] = [ma].[Id] ");
            query.Append(" LEFT JOIN [dbo].[Warranties] AS [w] ON [rr].[Id] = [w].[RequestId] ");
            query.Append(" LEFT JOIN [dbo].[Warehouses] AS [wo] ON CASE [rr].[OriginId] ");
            query.Append(" 		WHEN '84' ");
            query.Append(" 			THEN '88' ");
            query.Append(" 		WHEN '83' ");
            query.Append(" 			THEN '87' ");
            query.Append(" 		ELSE [rr].[OriginId] ");
            query.Append(" 		END = [wo].[Id] ");
            query.Append(" LEFT JOIN [dbo].[Warehouses] AS [ww] ON [rr].[WarehouseId] = [ww].[Id] ");
            query.Append(" LEFT JOIN [dbo].[Warehouses] AS [wd] ON [rr].[DestinationId] = [wd].[Id] ");
            query.Append(" LEFT JOIN [StatusFlowPivot] AS [pvt] ON [rr].[Id] = [pvt].[RequestId] ");

            #region card https://trello.com/c/hnsaxDtF
            // implementing filter for increase all requests performance

            // query.Append(" WHERE [rr].[Cancelled] = 0 ");
            query.Append(" WHERE 1 = 1 ");
            query.Append(" 	AND [rr].[Cancelled] = 0 ");
            
            var dateRequestedStart = DateTime.ParseExact(filters.DateRequestedStart, "yyyyMMdd", null);
            var dateRequestedEnd = DateTime.ParseExact(filters.DateRequestedEnd, "yyyyMMdd", null);
            query.AppendFormat(" AND (CAST([rr].[DateRequested] AS DATE) >= '{0:yyyy-MM-dd}' AND CAST([rr].[DateRequested] AS DATE) <= '{1:yyyy-MM-dd}') ", dateRequestedStart, dateRequestedEnd);


            if (filters.UseDelivery)
            {
                var dateDeliveryStart = DateTime.ParseExact(filters.DateDeliveryStart, "yyyyMMdd", null);
                var dateDeliveryEnd = DateTime.ParseExact(filters.DateDeliveryEnd, "yyyyMMdd", null);
                // query.AppendFormat(" AND ([rr].[DateDestination] >= '{0:yyyy-MM-dd}' AND [rr].[DateDestination] <= '{1:yyyy-MM-dd}') ", dateDeliveryStart, dateDeliveryEnd);

                query.Append(" AND EXISTS ( SELECT 1 FROM [dbo].[RequestFlows] AS [rf1] ");
                query.Append(" WHERE [rr].[Id] = [rf1].[RequestId] ");
                query.Append(" AND [rf1].[Status] IN (13, 7, 15) ");
                query.AppendFormat(" AND ( CAST([rf1].[DateRequested] AS DATE) >= '{0:yyyy-MM-dd}' ", dateDeliveryStart);
                query.AppendFormat(" AND CAST([rf1].[DateRequested] AS DATE) <= '{0:yyyy-MM-dd}')) ", dateDeliveryEnd);
            }

            if (filters.UseStatus)
            {
                query.AppendFormat(" AND [rr].[Status] IN ({0}) ", string.Join(",", filters.Status.Cast<int>()));
            }

            if (filters.UseOrigin)
            {
                query.AppendFormat(" AND [rr].[OriginId] IN ({0}) ", string.Join(",", filters.Origin));
            }

            if (filters.UseDestination)
            {
                query.AppendFormat(" AND [rr].[DestinationId] IN ({0}) ", string.Join(",", filters.Destination));
            }

            #endregion

            query.Append(" GROUP BY [rr].[Id] ");
            query.Append(" 	, [rr].[ModelId] ");
            query.Append(" 	, [m].[Name] ");
            query.Append(" 	, [m].[TM] ");
            query.Append(" 	, [m].[PartNumber] ");
            query.Append(" 	, [m].[ModelTypeId] ");
            query.Append(" 	, [mt].[Name] ");
            query.Append(" 	, [m].[ManufacturerId] ");
            query.Append(" 	, [ma].[Name] ");
            query.Append(" 	, [rr].[SerialNumber] ");
            query.Append(" 	, [rr].[DateRequested] ");
            query.Append(" 	, [rr].[IsDOA] ");
            query.Append(" 	, [rr].[Invoice] ");
            query.Append(" 	, CASE [rr].[Status] ");
            query.Append(" 		WHEN 0 ");
            query.Append(" 			THEN 'Received' ");
            query.Append(" 		WHEN 1 ");
            query.Append(" 			THEN 'TrialPerformed' ");
            query.Append(" 		WHEN 2 ");
            query.Append(" 			THEN 'SentToRepair' ");
            query.Append(" 		WHEN 3 ");
            query.Append(" 			THEN 'Repaired' ");
            query.Append(" 		WHEN 4 ");
            query.Append(" 			THEN 'SentToCosmetic' ");
            query.Append(" 		WHEN 5 ");
            query.Append(" 			THEN 'CosmeticPerformed' ");
            query.Append(" 		WHEN 6 ");
            query.Append(" 			THEN 'SentToScrapEvaluation' ");
            query.Append(" 		WHEN 7 ");
            query.Append(" 			THEN 'SentToScrap' ");
            query.Append(" 		WHEN 8 ");
            query.Append(" 			THEN 'SentToDOA' ");
            query.Append(" 		WHEN 9 ");
            query.Append(" 			THEN 'SentToFinalInspection' ");
            query.Append(" 		WHEN 10 ");
            query.Append(" 			THEN 'FinalInspection' ");
            query.Append(" 		WHEN 11 ");
            query.Append(" 			THEN 'SentToCosmeticHold' ");
            query.Append(" 		WHEN 12 ");
            query.Append(" 			THEN 'SentBackToTrial' ");
            query.Append(" 		WHEN 13 ");
            query.Append(" 			THEN 'Delivered' ");
            query.Append(" 		WHEN 14 ");
            query.Append(" 			THEN 'SentToBgaScrapEvaluation' ");
            query.Append(" 		WHEN 15 ");
            query.Append(" 			THEN 'SentToBgaScrap' ");
            query.Append(" 		ELSE 'N/A' ");
            query.Append(" 		END ");
            query.Append(" 	, ISNULL(CASE [w].[InWarranty] ");
            query.Append(" 			WHEN 0 ");
            query.Append(" 				THEN 'Denied' ");
            query.Append(" 			WHEN 1 ");
            query.Append(" 				THEN ( ");
            query.Append(" 						CASE [rr].[Status] ");
            query.Append(" 							WHEN 0 ");
            query.Append(" 								THEN 'To be confirmed' ");
            query.Append(" 							ELSE 'Yes' ");
            query.Append(" 							END ");
            query.Append(" 						) ");
            query.Append(" 			END, 'No') ");
            query.Append(" 	, [rr].[BatchId] ");
            query.Append(" 	, [wo].[Id] + ' - ' + [wo].[Name] ");
            query.Append(" 	, [ww].[Id] + ' - ' + [ww].[Name] ");
            query.Append(" 	, [rr].[DateWarehouse] ");
            query.Append(" 	, [wd].[Id] + ' - ' + [wd].[Name] ");
            //query.Append(" 	[re].[Description]  ");
            query.Append(" ORDER BY [rr].[Id] ");

            var model = db.Database
                .SqlQuery<RefurbRequestExportModel>(query.ToString())
                .ToList();

            return new CsvActionResult<RefurbRequestExportModel>(model,
                string.Format("all-requests.{0:yyyyMMddHHmm}.csv", DateTime.Now),
                ';');
        }


        protected override void Dispose(bool disposing)
        {
            objects.Dispose();
            base.Dispose(disposing);
        }
    }

    public class RefurbRequestExportModel
    {
        public int RequestId { get; set; }
        public int ModelId { get; set; }
        public string Model { get; set; }
        public string SAPCode { get; set; }
        public string PartNumber { get; set; }
        public int ModelTypeId { get; set; }
        public string ModelType { get; set; }
        public int? ManufacturerId { get; set; }
        public string Manufacturer { get; set; }
        public string SerialNumber { get; set; }
        public bool isDOA { get; set; }
        public DateTime DateRequested { get; set; }
        public string Status { get; set; }
        public string Supply { get; set; }
        public string InWarranty { get; set; }
        public int? BatchId { get; set; }
        public DateTime? ReceivedAt { get; set; }
        public string Origin { get; set; }
        public DateTime? TrialPerformedAt { get; set; }
        public string Warehouse { get; set; }
        public DateTime? DateWarehouse { get; set; }
        public DateTime? SentToRepairAt { get; set; }
        public DateTime? RepairedAt { get; set; }
        public string RepairDescription { get; set; }
        public string RepairChecklist { get; set; }
        public DateTime? SentToCosmeticAt { get; set; }
        public DateTime? CosmeticPerformedAt { get; set; }
        public DateTime? SentToScrapEvaluationAt { get; set; }
        public DateTime? SentToBgaScrapEvaluationAt { get; set; }
        public DateTime? SentToScrapAt { get; set; }
        public DateTime? SentToBgaScrapAt { get; set; }
        public DateTime? SentToDOAAt { get; set; }
        public DateTime? SentToFinalInspectionAt { get; set; }
        public DateTime? FinalInspectionAt { get; set; }
        public DateTime? SentToCosmeticHoldAt { get; set; }
        public DateTime? SentBackToTrialAt { get; set; }
        public DateTime? Delivered { get; set; }
        public string Destination { get; set; }
        public string Invoice { get; set; }
    }

    public class RefurRequestExportCsvModel
    {
        [DisplayName("Material")]
        public string TM { get; set; }

        [DisplayName("Denominação")]
        public string ModelName { get; set; }

        [DisplayName("Tipo")]
        public string ModelTypeName { get; set; }

        [DisplayName("Nº Série")]
        public string SerialNumber { get; set; }
    }

    public class AllRequestsFilter
    {
        public string DateRequestedStart { get; set; }
        public string DateRequestedEnd { get; set; }
        public string DateDeliveryStart { get; set; }
        public string DateDeliveryEnd { get; set; }
        public List<RequestFlowStatus> Status { get; set; }
        public List<string> Origin { get; set; }
        public List<string> Destination { get; set; }

        public bool UseRequested
        {
            get
            {
                return !string.IsNullOrEmpty(DateRequestedStart);
            }
        }

        public bool UseDelivery
        {
            get
            {
                return !string.IsNullOrEmpty(DateDeliveryStart);
            }
        }

        public bool UseStatus
        {
            get
            {
                return Status != null && Status.Count > 0;
            }
        }

        public bool UseOrigin
        {
            get
            {
                var useOrigin = Origin != null && Origin.Count > 0;
                if (useOrigin)
                {
                    Origin = Origin.Where(s => !string.IsNullOrWhiteSpace(s)).Distinct().ToList();
                }
                return useOrigin;
            }
        }

        public bool UseDestination
        {
            get
            {
                var useDestination = Destination != null && Destination.Count > 0;
                if (useDestination)
                {
                    Destination = Destination.Where(s => !string.IsNullOrWhiteSpace(s)).Distinct().ToList();
                }
                return useDestination;
            }
        }

    }
}