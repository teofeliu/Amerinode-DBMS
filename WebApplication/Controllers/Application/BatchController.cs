using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Data.Entity;
using WebApplication.Models.Application;
using System.Collections.Generic;
using Rotativa;
using Rotativa.Options;
using System;
using WebApplication.Attributes;
using NPOI.XSSF.UserModel;
using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using System.IO;
using WebApplication.Extensions;
using DataTables.AspNet.Core;
using DataTables.AspNet.Mvc5;
using System.Reflection;
using System.Linq.Dynamic;
using System.ComponentModel;

namespace WebApplication.Controllers.Application
{
    [AuthorizePermissions(Resource = "Batch")]
    public class BatchController : BaseAdminController<Batch>
    {
        BatchManager objects = new BatchManager();

        //Chamado pelo botão do SideMenu. Garante que carregue sem parametros
        public ActionResult CreateNew() { return RedirectToAction("Create"); }


        [AuthorizePermissions(Resource = "Batch", Operation = "Write")]
        public override ActionResult Create(int? id)
        {
            object selected = null;
            var model = new Batch(User.Identity.Name);
            ViewBag.Status = null;
            if (id != null)
            {
                model = db.Set<Batch>().Find(id);
                ViewBag.Status = model.Status;
                selected = model.ModelId;
            }
            ViewBag.Models = PopulateDropDown(typeof(Model), "Id", "Name", selected);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthorizePermissions(Resource = "Batch", Operation = "Write")]
        public override ActionResult Create(Batch model, int? id, HttpPostedFileBase file)
        {
            object selected = model.ModelId;
            ViewBag.Models = PopulateDropDown(typeof(Model), "Id", "Name", selected);
            ViewBag.Status = model.Status;
            if (ModelState.IsValid)
            {
                if (id == null)
                {
                    model.Status = BatchStatus.PendingReview;
                    db.Set<Batch>().Add(model);
                }
                else if (model.Status == BatchStatus.PendingReview)
                {
                    model.Status = BatchStatus.Conferred;
                    db.Entry(model).State = EntityState.Modified;
                }
                else if (model.Status == BatchStatus.Conferred && model.IsQuantitiesOk())
                {
                    model.Status = BatchStatus.Tested;
                    model.FunctionalTestDate = System.DateTime.Now;
                    model.FunctionalTestUserId = User.Identity.Name;
                    db.Entry(model).State = EntityState.Modified;
                }
                else
                {
                    ViewBag.Message = "Seems like your entries are not okay. Please verify if everything is fine with your quantities before saving.";
                    return View(model);
                }
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            ViewBag.Message = "Please, check your entries.";
            return View(model);
        }

        [AuthorizePermissions(Resource = "Batch", Operation = "Read")]
        public ActionResult ByStatus(BatchStatus Status)
        {
            //var results = objects.BatchesByStatus(Status);
            ViewBag.Status = Status;

            if (Status == BatchStatus.Conferred)
                ViewBag.Menu = "conferred";
            else if (Status == BatchStatus.Tested)
                ViewBag.Menu = "tested";
            else if (Status == BatchStatus.PendingReview)
                ViewBag.Menu = "pending";

            return View();
        }

        [AuthorizePermissions(Resource = "Batch", Operation = "Read")]
        public ActionResult Divergents()
        {
            ViewBag.Menu = "divergent";
            ViewBag.Status = BatchStatus.PendingReview;

            return View();
        }

        [AuthorizePermissions(Resource = "Batch", Operation = "Write")]
        public ActionResult AsPdf(int? id)
        {
            var model = db.Set<Batch>().Find(id);
            return View(model);

            //return new ViewAsPdf
            //{
            //    Model = model,
            //    FileName = string.Format("agreement-report.{0:yyyyMMddHHmm}.pdf", DateTime.Now),
            //    PageSize = Size.A4
            //};


        }

        [AuthorizePermissions(Resource = "Batch", Operation = "Write")]
        public ActionResult BatchReport(int? id)
        {
            var model = db.Set<Batch>().Find(id);
            return View(model);         
        }

        [AuthorizePermissions(Resource = "Batch", Operation = "Read")]
        public ActionResult Details(int id)
        {
            return View(objects.GetBatch(id));
        }

        [AuthorizePermissions(Resource = "Batch", Operation = "Write")]
        public ActionResult ImportFromExcel()
        {
            return View();
        }

        [HttpPost]
        [AuthorizePermissions(Resource = "Batch", Operation = "Write")]
        public ActionResult ImportFromExcel(HttpPostedFileBase File)
        {
            if (!validadeFile(File)) return View();

            try
            {
                objects.CreateBatchFromExcelStream(File.InputStream, Path.GetExtension(File.FileName), User.Identity.Name);
            }
            catch (Exception ex)
            {
                ViewBag.Message = (ex.InnerException != null)
                    ? ex.InnerException.Message
                    : ex.Message;
                return View();
            }


            return RedirectToAction("Index");

        }

        [AuthorizePermissions(Resource = "Batch", Operation = "Delete")]
        public override ActionResult Delete(int? id)
        {
            return base.Delete(id);
        }

        [AuthorizePermissions(Resource = "Batch", Operation = "Delete")]
        public override ActionResult DeleteConfirmed(int id)
        {
            // return base.DeleteConfirmed(id);

            var batch = db.Batches.Find(id);

            try
            {

                #region deprecated code https://trello.com/c/57TkxeGQ
                //var batchItems = db.BatchItems.RemoveRange(
                //    db.BatchItems.Where(bi => bi.BatchId == batch.Id).ToList());

                //db.Batches.Remove(batch);
                //db.SaveChanges();
                #endregion

                batch = objects.DeleteBatch(id);


                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                ViewBag.Message = (ex.InnerException != null)
                    ? ex.InnerException.Message
                    : ex.Message;
            }

            return View("Delete", batch);
        }

        private bool validadeFile(HttpPostedFileBase File)
        {
            string[] allowedExtensions = { ".xlsx", ".xls" };

            if (File == null)
            {
                ViewBag.Message = "No file selected.";
                return false;
            }
            string extension = Path.GetExtension(File.FileName);
            if (!allowedExtensions.Contains(extension))
            {
                ViewBag.Message = "File extension not supported.";
                return false;
            }

            return true;
        }


        [AuthorizePermissions(Resource = "Batch", Operation = "Read")]
        public JsonResult PageData(IDataTablesRequest request, BatchStatus? status, bool? isDivergent)
        {
            IQueryable<Batch> data = db.Batches;

            // statement implemented by the card https://trello.com/c/57TkxeGQ
            if (String.IsNullOrEmpty(request.Search.Value))
            {
                // excluding all batches that have deleted 
                data = data.Where(b => b.Status != BatchStatus.Deleted);
            }

            if (status != null)
            {
                data = data.Where(b => b.Status == status);
            }
            if (isDivergent != null && (bool)isDivergent)
            {
                data = data.Where(b => b.Quantity != b.QuantityConferred);
            }

            var filtered = data.AsQueryable();

            var attrs = GetFilterFields();
            if (!String.IsNullOrEmpty(request.Search.Value))
            {
                List<string> where = new List<string>();
                object[] param = new object[attrs.Count];
                Type t = new Batch().GetType();

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
                    if (prop.PropertyType == typeof(BatchStatus))
                    {
                        BatchStatus val;
                        if (Enum.TryParse<BatchStatus>(request.Search.Value, true, out val))
                        {
                            where.Add(attrs[i] + " = @" + i + "");
                            param[i] = (BatchStatus)val;
                        }
                    }
                }

                filtered = filtered.Where(String.Join(" || ", where), param);
            }

            // card https://trello.com/c/QZzvlT2Z
            // enable sorting features
            var colSorting = request.Columns
                .FirstOrDefault(c => c.Sort != null);
            if (colSorting == null)
            {
                filtered = filtered.OrderBy(f => f.Id);
            }
            else
            {
                switch (colSorting.Field)
                {
                    case "date":
                        if (colSorting.Sort.Direction == SortDirection.Ascending)
                            filtered = filtered.OrderBy(f => f.Date);
                        else
                            filtered = filtered.OrderByDescending(f => f.Date);
                        break;
                    default:
                        if (colSorting.Sort.Direction == SortDirection.Ascending)
                            filtered = filtered.OrderBy(f => f.Id);
                        else
                            filtered = filtered.OrderByDescending(f => f.Id);
                        break;
                }
            }

                
            var dataPage = filtered
                //.OrderBy(b => b.Id)
                .Skip(request.Start)
                .Take(request.Length)
                .ToList()
                .Select(b => new
                {
                    b.Id,
                    Model = (b.Model != null) ? b.Model.Description : String.Empty,
                    Received = b.Quantity,
                    Conferred = b.QuantityConferred,
                    IsDivergent = b.GetDivergentVerbose(),
                    IsDivergentCss = b.GetDivergentCssClass(),
                    Approved = b.QuantityApproved,
                    DisapprovedByFunctionalTest = b.QuantityDisapproved,
                    DisapprovedByCosmetic = b.QuantityDisapprovedByCosmetic,
                    b.Date,
                    Status = b.Status.ToString(),
                    StatusDescription = Utility.GetDescriptionFromEnumValue(b.Status)
                });

            var response = DataTablesResponse.Create(request, data.Count(), filtered.Count(), dataPage);

            return new DataTablesJsonResult(response);
        }


        #region Conferencia de Lotes Por SN
        [AuthorizePermissions(Resource = "BatchConference", Operation = "Write")]
        public ActionResult BatchConference(int id)
        {
            return View(objects.GetBatch(id));
        }

        [HttpPost]
        [ActionName("BatchConference")]
        [AuthorizePermissions(Resource = "BatchConference", Operation = "Write")]
        public ActionResult BatchConferenceCreate(int id)
        {
            try
            {
                var batch = db.Batches.Find(id);

                var notConferrefCount = objects.GetBatchItemsNotConferred(batch.Id).Count();
                var notFoundCount = objects.GetBatchItemsNotFound(batch.Id).Count();
                if (notConferrefCount > 0)
                {
                    throw new Exception("To perform this action you must to be conferred all items from list");
                }
                if (notFoundCount > 0)
                {
                    throw new Exception("To perform this action you must to be resolve all items not founded from list.");
                }


                var batchItems = db.BatchItems
                    .Where(bi => bi.BatchId == batch.Id)
                    .ToList();

                batch.Status = BatchStatus.Conferred;
                var requestManager = new RequestManager();

                try
                {
                    foreach (var item in batchItems)
                    {
                        requestManager.ReceiveRequest(new RefurbRequest
                        {
                            ModelId = (int)item.ModelId,
                            SerialNumber = item.SerialNumber,
                            DateRequested = DateTime.Now,
                            UserId = User.Identity.Name,
                            Status = RequestFlowStatus.Received,
                            BatchId = item.BatchId,
                            Cancelled = false,
                            LastUpdated = DateTime.Now,
                            OriginId = item.OriginId,
                            DateOrigin = DateTime.Now,
                            WarehouseId = item.DestinationId,
                            DateWarehouse = DateTime.Now,
                            Invoice = item.Invoice
                        }, null, null);
                    }
                }
                catch (Exception ruleValidation)
                {
                    ViewBag.Message = (ruleValidation.InnerException != null)
                        ? ruleValidation.InnerException.Message
                        : ruleValidation.Message;
                    return View(objects.GetBatch(id));
                }

                db.SaveChanges();
            }
            catch (Exception ex)
            {
                ViewBag.Message = string.Format("{0}\r\nInformation to support: {1}",
                    "Cannot create requests for this batch. Please, review your items and try again.",
                    (ex.InnerException != null) ? ex.InnerException.Message : ex.Message);

                return View(objects.GetBatch(id));
            }

            return RedirectToAction("Index");
        }

        [AuthorizePermissions(Resource = "BatchConference", Operation = "Write")]
        public JsonResult SNConference(int id)
        {
            var sn = Request.Form["sn"];

            // removido porque nem todos os seriais tem 15 caracteres
            //if (sn.Length != 15)
            //{
            //    return Json(new { Invalid = true }, JsonRequestBehavior.AllowGet);
            //}

            var bi = objects.VerifySerialNumber(id, sn);
            var ret = new
            {
                Invalid = false,
                BatchId = bi.BatchId,

                DateTime = bi.Date,
                DateConferred = bi.DateConferred,
                SerialNumber = bi.SerialNumber,
                Found = bi.Found,
                FoundVerbal = bi.Found ? "Yes" : "No",
                Conferred = bi.Conferred,
                ConferredVerbal = bi.Conferred ? "Yes" : "No",
                ModelId = bi.ModelId,
                ConferredCount = objects.GetBatchItemsConferred(id).Count(),
                NotFoundCount = objects.GetBatchItemsNotFound(id).Count()
            };
            return Json(ret, JsonRequestBehavior.AllowGet);
        }


        [AuthorizePermissions(Resource = "BatchConference", Operation = "Write")]
        public JsonResult BatchConferenceItemsFound(int id)
        {
            var model = objects
                .GetBatchItemsFound(id)
                .Select(bi => new
                {
                    bi.Id,
                    ModelName = (bi.Model != null) ? bi.Model.Name : string.Empty,
                    bi.SerialNumber,
                    bi.Date,
                    Origin = (bi.Origin != null) ? bi.Origin.ToString() : string.Empty,
                    Destination = (bi.Destination != null) ? bi.Destination.ToString() : string.Empty
                });

            return Json(model,
                JsonRequestBehavior.AllowGet);
        }

        [AuthorizePermissions(Resource = "BatchConference", Operation = "Write")]
        public JsonResult BatchConferenceNotConferred(int id)
        {
            var model = objects
                .GetBatchItemsNotConferred(id)
                .Select(bi => new
                {
                    bi.Id,
                    ModelName = (bi.Model != null) ? bi.Model.Name : string.Empty,
                    bi.SerialNumber,
                    bi.Date,
                    Origin = (bi.Origin != null) ? bi.Origin.ToString() : string.Empty,
                    Destination = (bi.Destination != null) ? bi.Destination.ToString() : string.Empty
                });

            return Json(model,
                JsonRequestBehavior.AllowGet);
        }

        [AuthorizePermissions(Resource = "BatchConference", Operation = "Write")]
        public JsonResult BatchConferenceNotFound(int id)
        {
            var model = objects
                .GetBatchItemsNotFound(id)
                .Select(bi => new
                {
                    bi.Id,
                    ModelName = (bi.Model != null) ? bi.Model.Name : string.Empty,
                    bi.SerialNumber,
                    bi.Date,
                    Origin = (bi.Origin != null) ? bi.Origin.ToString() : string.Empty,
                    Destination = (bi.Destination != null) ? bi.Destination.ToString() : string.Empty
                });

            return Json(model,
                JsonRequestBehavior.AllowGet);
        }

        [AuthorizePermissions(Resource = "BatchConference", Operation = "Write")]
        public ActionResult BatchItemDelete(int id, int itemId)
        {
            return View(objects.GetBatchItemById(id, itemId));
        }

        [AuthorizePermissions(Resource = "BatchConference", Operation = "Write")]
        [HttpPost]
        public ActionResult BatchItemDeleteConfirmed(int id, int itemId)
        {
            var result = objects.DeleteBatchItem(id, itemId);
            if (!result)
            {
                ViewData["error"] = "Cannot delete this item batch";
                RedirectToAction("BatchItemDelete", new { id = id, itemId = itemId });
            }

            return RedirectToAction("BatchConference", new { id = id });

        }

        [AuthorizePermissions(Resource = "BatchConference", Operation = "Write")]
        public ActionResult BatchItemEdit(int id)
        {
            var model = objects.GetBatchItemById(id);

            ViewBag.Models = PopulateDropDown(typeof(Model), "Id", "Name");

            return View(model);
        }

        [HttpPost]
        [AuthorizePermissions(Resource = "BatchConference", Operation = "Write")]
        public ActionResult BatchItemEdit(int id, BatchItem model)
        {
            var previous = db.BatchItems.Find(model.Id);
            if (previous != null)
            {
                previous.ModelId = model.ModelId;
                previous.Conferred = false;
                previous.Found = false;
                db.SaveChanges();

                return RedirectToAction("BatchConference", new { id = previous.BatchId });
            }

            ViewBag.Models = PopulateDropDown(typeof(Model), "Id", "Name", model.ModelId);

            return View(model);
        }

        #endregion


        #region Delivery Batches

        [HttpGet]
        [AuthorizePermissions(Resource = "BatchDelivery", Operation = "Read")]
        public ActionResult Delivery()
        {
            return View();

            return View(objects.GetDeliveryBatches());
        }


        [HttpPost]
        [AuthorizePermissions(Resource = "BatchDelivery", Operation = "Read")]
        public JsonResult PagedDelivery(IDataTablesRequest request)
        {
            // card https://trello.com/c/QZzvlT2Z
            // enable sorting features 


            //var data = db.DeliveryBatches
            //    .OrderBy(d => (d.Delivered == null) ? d.Delivered : d.Created);
            IOrderedQueryable<DeliveryBatch> data = db.DeliveryBatches;

            var colSorting = request.Columns.FirstOrDefault(c => c.Sort != null);
            if (colSorting == null)
            {
                data = data.OrderBy(d => d.Id);
            }
            else
            {
                switch(colSorting.Field)
                {
                    case "created":
                        if (colSorting.Sort.Direction == SortDirection.Ascending)
                            data = data.OrderBy(d => d.Created);
                        else
                            data = data.OrderByDescending(d => d.Created);
                        break;
                    case "delivered":
                        if (colSorting.Sort.Direction == SortDirection.Ascending)
                            data = data.OrderBy(d => d.Delivered);
                        else
                            data = data.OrderByDescending(d => d.Delivered);
                        break;
                    case "cancelled":
                        if (colSorting.Sort.Direction == SortDirection.Ascending)
                            data = data.OrderBy(d => d.Cancelled);
                        else
                            data = data.OrderByDescending(d => d.Cancelled);
                        break;
                    case "id":
                        if (colSorting.Sort.Direction == SortDirection.Ascending)
                            data = data.OrderBy(d => d.Id);
                        else
                            data = data.OrderByDescending(d => d.Id);
                        break;
                }   
            }
            // eo card https://trello.com/c/QZzvlT2Z


            var page = data
                .Skip(request.Start)
                .Take(request.Length)
                .ToList()
                .Select(dr => new
                {
                    dr.Id,
                    Code = dr.GetCode,
                    dr.UserId,
                    dr.Created,
                    dr.Delivered,
                    dr.UserDeliveryId,
                    dr.Cancelled,
                    dr.UserCancelId,
                    Status = dr.Status.ToString(),
                    StatusDescription = Utility.GetDescriptionFromEnumValue(dr.Status)
                });

            var count = data.Count();

            var response = DataTablesResponse.Create(request, count, count, page);

            return new DataTablesJsonResult(response);
        }


        [HttpGet]
        [AuthorizePermissions(Resource = "BatchDelivery", Operation = "Write")]
        public ActionResult CreateDeliveryBatch(int? id)
        {
            var deliveryBatch = objects.GetOrCreateDeliveryBatch(id, User);

            return View(deliveryBatch);
        }

        [HttpGet]
        [AuthorizePermissions(Resource = "BatchDelivery", Operation = "Delete")]
        public ActionResult CancelDeliveryBatch(int id)
        {
            return View(objects.GetOrCreateDeliveryBatch(id, User));
        }

        [HttpPost]
        [AuthorizePermissions(Resource = "BatchDelivery", Operation = "Delete")]
        [ActionName("CancelDeliveryBatch")]
        public ActionResult CancelDeliveryBatchConfirmed(int id)
        {
            try
            {
                if (objects.PerformCancelDeliveryBatch(id, User))
                {
                    return RedirectToActionPermanent("Delivery");
                }

            }
            catch (Exception ex)
            {
                ViewBag.Message = String.Format("Cannot cancel this batch. Information to support: {0}",
                    (ex.InnerException != null) ? ex.InnerException.Message : ex.Message);
            }

            return View(objects.GetOrCreateDeliveryBatch(id, User));
        }

        [HttpPost]
        [AuthorizePermissions(Resource = "BatchDelivery", Operation = "Write")]
        public JsonResult AddDeliveryBatchItem(int id, string sn)
        {
            var result = objects.AddDeliveryBatchItem(id, sn, User);
            return Json(result);
        }

        [HttpPost]
        [AuthorizePermissions(Resource = "BatchDelivery", Operation = "Delete")]
        public JsonResult DeleteDeliveryBatchItem(int id)
        {
            return Json(objects.RemoveDeliveryBatchItem(id));
        }

        [HttpGet]
        [AuthorizePermissions(Resource = "BatchDelivery", Operation = "Write")]
        public ActionResult ProcessDeliveryBatch(int id)
        {
            var model = objects.GetOrCreateDeliveryBatch(id, User);
            model.Delivered = DateTime.Now;
            return View(model);
        }

        [HttpPost]
        [AuthorizePermissions(Resource = "BatchDelivery", Operation = "Write")]
        [ValidateAntiForgeryToken]
        public ActionResult ProcessDeliveryBatch(int id, DeliveryBatch model)
        {
            DeliveryBatch batch;
            try
            {
                batch = objects.PerformDeliveryBatch(id, User, model.Delivered);
            }
            catch (InvalidOperationException ex)
            {
                ViewBag.Message = ex.Message;
                return View(model);
            }
            catch (Exception ex)
            {
                ViewBag.Message = String.Format("Oops, an error is caught. Please call the support with this information: {0}",
                    ex.InnerException != null ? ex.InnerException.Message : ex.Message);
                return View(model);
            }

            return RedirectToActionPermanent("Delivery");
        }

        [HttpGet]
        [AuthorizePermissions(Resource = "BatchDelivery", Operation = "Write")]
        public CsvActionResult<DeliveryBatchExport> DeliveryBatchAsCsv(int id)
        {
            var batch = db.DeliveryBatches.Find(id);
            var items = db.DeliveryBatchItems
                .Where(d => d.DeliveryBatchId == batch.Id)
                .Select(d => new { item = d, repair = db.Repairs.FirstOrDefault(r => r.RequestId == d.RefurbRequestId) })
                .ToList()
                .Select(d => new DeliveryBatchExport
                {
                    DateRequested = d.item.RefurbRequest.DateRequested,
                    SAPCode = d.item.RefurbRequest.Model.TM,
                    Model = d.item.RefurbRequest.Model.Name,
                    SerialNumber = d.item.RefurbRequest.SerialNumber,
                    RepairedAt = (d.repair != null) ? d.repair.Date : (DateTime?)null,
                    RepairedDescription = (d.repair != null) ? d.repair.Description : String.Empty,
                    BatchDeliveredDate = d.item.DeliveryBatch.Delivered,
                    Aging = ((DateTime)d.item.DeliveryBatch.Delivered).Subtract(d.item.RefurbRequest.DateRequested).Days,
                    OriginId = d.item.RefurbRequest.WarehouseId,
                    Origin = (d.item.RefurbRequest.Warehouse != null)
                        ? d.item.RefurbRequest.Warehouse.Name
                        : string.Empty,
                    DestinationId = d.item.RefurbRequest.DestinationId,
                    Destination = (d.item.RefurbRequest.Destination != null)
                        ? d.item.RefurbRequest.Destination.Name
                        : string.Empty
                })
                .ToList();

            return new CsvActionResult<DeliveryBatchExport>(items,
                string.Format("{0}.csv", batch.GetCode),
                ';');
        }
        #endregion


        #region Scrap batches
        [HttpGet]
        [AuthorizePermissions(Resource = "Batch", Operation = "Read")]
        public ActionResult ScrapBatch()
        {
            return View();
        }

        [HttpPost]
        [AuthorizePermissions(Resource = "Batch", Operation = "Read")]
        public JsonResult PagedScrap(IDataTablesRequest request)
        {
            // card https://trello.com/c/QZzvlT2Z
            // enable sorting features 


            IOrderedQueryable<ScrapBatch> data = db.ScrapBatches;

            var colSorting = request.Columns.FirstOrDefault(c => c.Sort != null);
            if (colSorting == null)
            {
                data = data.OrderBy(d => d.Id);
            }
            else
            {
                switch (colSorting.Field)
                {
                    case "created":
                        if (colSorting.Sort.Direction == SortDirection.Ascending)
                            data = data.OrderBy(d => d.Created);
                        else
                            data = data.OrderByDescending(d => d.Created);
                        break;
                    case "delivered":
                        if (colSorting.Sort.Direction == SortDirection.Ascending)
                            data = data.OrderBy(d => d.Delivered);
                        else
                            data = data.OrderByDescending(d => d.Delivered);
                        break;
                    case "cancelled":
                        if (colSorting.Sort.Direction == SortDirection.Ascending)
                            data = data.OrderBy(d => d.Cancelled);
                        else
                            data = data.OrderByDescending(d => d.Cancelled);
                        break;
                    case "id":
                        if (colSorting.Sort.Direction == SortDirection.Ascending)
                            data = data.OrderBy(d => d.Id);
                        else
                            data = data.OrderByDescending(d => d.Id);
                        break;
                }
            }
            // eo card https://trello.com/c/QZzvlT2Z
            

            var page = data
                .Skip(request.Start)
                .Take(request.Length)
                .ToList()
                .Select(dr => new
                {
                    dr.Id,
                    Code = dr.GetCode,
                    dr.UserId,
                    dr.Created,
                    dr.Delivered,
                    dr.UserDeliveryId,
                    dr.Cancelled,
                    dr.UserCancelId,
                    Status = dr.Status.ToString(),
                    StatusDescription = Utility.GetDescriptionFromEnumValue(dr.Status)
                });

            var count = data.Count();

            var response = DataTablesResponse.Create(request, count, count, page);

            return new DataTablesJsonResult(response);
        }

        [HttpGet]
        [AuthorizePermissions(Resource = "Batch", Operation = "Write")]
        public ActionResult CreateScrapBatch(int? id)
        {
            var scrapBatch = objects.GetOrCreateScrapBatch(id, User);

            return View(scrapBatch);
        }

        [HttpPost]
        [AuthorizePermissions(Resource = "Batch", Operation = "Write")]
        public JsonResult AddScrapBatchItem(int id, string sn)
        {
            var result = objects.AddScrapBatchItem(id, sn, User);
            return Json(result);
        }

        [HttpPost]
        [AuthorizePermissions(Resource = "Batch", Operation = "Write")]
        public JsonResult DeleteScrapBatchItem(int id)
        {
            return Json(objects.RemoveScrapBatchItem(id));
        }

        [HttpGet]
        [AuthorizePermissions(Resource = "Batch", Operation = "Delete")]
        public ActionResult CancelScrapBatch(int id)
        {
            return View(objects.GetOrCreateScrapBatch(id, User));
        }

        [HttpPost]
        [AuthorizePermissions(Resource = "Batch", Operation = "Delete")]
        [ActionName("CancelScrapBatch")]
        public ActionResult CancelScrapBatchConfirmed(int id)
        {
            try
            {
                if (objects.PerformCancelScrapBatch(id, User))
                {
                    return RedirectToActionPermanent("ScrapBatch");
                }

            }
            catch (Exception ex)
            {
                ViewBag.Message = String.Format("Cannot cancel this batch. Information to support: {0}",
                    (ex.InnerException != null) ? ex.InnerException.Message : ex.Message);
            }

            return View(objects.GetOrCreateScrapBatch(id, User));
        }

        [HttpGet]
        [AuthorizePermissions(Resource = "Batch", Operation = "Write")]
        public ActionResult ProcessScrapBatch(int id)
        {
            var model = objects.GetOrCreateScrapBatch(id, User);
            model.Delivered = DateTime.Now;
            return View(model);
        }

        [HttpPost]
        [AuthorizePermissions(Resource = "Batch", Operation = "Write")]
        [ValidateAntiForgeryToken]
        public ActionResult ProcessScrapBatch(int id, ScrapBatch model)
        {
            ScrapBatch batch;
            try
            {
                batch = objects.PerformScrapBatch(id, User, model.Delivered);
            }
            catch (InvalidOperationException ex)
            {
                ViewBag.Message = ex.Message;
                return View(model);
            }
            catch (Exception ex)
            {
                ViewBag.Message = String.Format("Oops, an error is caught. Please call the support " +
                    "with this information: {0}",
                    ex.InnerException != null ? ex.InnerException.Message : ex.Message);
                return View(model);
            }

            return RedirectToActionPermanent("ScrapBatch");
        }

        [HttpGet]
        [AuthorizePermissions(Resource = "Batch", Operation = "Write")]
        public CsvActionResult<ScrapBatchExport> ScrapBatchAsCsv(int id)
        {
            var batch = db.ScrapBatches.Find(id);
            var items = db.ScrapBatchItems
                .Where(d => d.ScrapBatchId == batch.Id)
                .Select(d => new { item = d, repair = db.Repairs.FirstOrDefault(r => r.RequestId == d.RefurbRequestId) })
                .ToList()
                .Select(d => new ScrapBatchExport
                {
                    TM = d.item.RefurbRequest.Model.TM,
                    ModelName = d.item.RefurbRequest.Model.Name,
                    ModelTypeName = d.item.RefurbRequest.Model.Type.Name,
                    SerialNumber = d.item.RefurbRequest.SerialNumber,
                    OriginId = d.item.RefurbRequest.WarehouseId,
                    Origin = (d.item.RefurbRequest.Warehouse != null)
                        ? d.item.RefurbRequest.Warehouse.Name
                        : string.Empty,
                    DestinationId = d.item.RefurbRequest.DestinationId,
                    Destination = (d.item.RefurbRequest.Destination != null)
                        ? d.item.RefurbRequest.Destination.Name
                        : string.Empty
                })
                .ToList();

            return new CsvActionResult<ScrapBatchExport>(items,
                string.Format("{0}.csv", batch.GetCode),
                ';');
        }
        #endregion

        #region BGA Scrap Batches
        [HttpGet]
        [AuthorizePermissions(Resource = "Batch", Operation = "Read")]
        public ActionResult BgaScrapBatch()
        {
            return View();
        }

        [HttpPost]
        [AuthorizePermissions(Resource = "Batch", Operation = "Read")]
        public JsonResult PagedBgaScrap(IDataTablesRequest request)
        {
            // card https://trello.com/c/QZzvlT2Z
            // enable sorting features 


            IOrderedQueryable<BgaScrapBatch> data = db.BgaScrapBatches;

            var colSorting = request.Columns.FirstOrDefault(c => c.Sort != null);
            if (colSorting == null)
            {
                data = data.OrderBy(d => d.Id);
            }
            else
            {
                switch (colSorting.Field)
                {
                    case "created":
                        if (colSorting.Sort.Direction == SortDirection.Ascending)
                            data = data.OrderBy(d => d.Created);
                        else
                            data = data.OrderByDescending(d => d.Created);
                        break;
                    case "delivered":
                        if (colSorting.Sort.Direction == SortDirection.Ascending)
                            data = data.OrderBy(d => d.Delivered);
                        else
                            data = data.OrderByDescending(d => d.Delivered);
                        break;
                    case "cancelled":
                        if (colSorting.Sort.Direction == SortDirection.Ascending)
                            data = data.OrderBy(d => d.Cancelled);
                        else
                            data = data.OrderByDescending(d => d.Cancelled);
                        break;
                    case "id":
                        if (colSorting.Sort.Direction == SortDirection.Ascending)
                            data = data.OrderBy(d => d.Id);
                        else
                            data = data.OrderByDescending(d => d.Id);
                        break;
                }
            }
            // eo card https://trello.com/c/QZzvlT2Z


            var page = data
                .Skip(request.Start)
                .Take(request.Length)
                .ToList()
                .Select(dr => new
                {
                    dr.Id,
                    Code = dr.GetCode,
                    dr.UserId,
                    dr.Created,
                    dr.Delivered,
                    dr.UserDeliveryId,
                    dr.Cancelled,
                    dr.UserCancelId,
                    Status = dr.Status.ToString(),
                    StatusDescription = Utility.GetDescriptionFromEnumValue(dr.Status)
                });

            var count = data.Count();

            var response = DataTablesResponse.Create(request, count, count, page);

            return new DataTablesJsonResult(response);
        }

        [HttpGet]
        [AuthorizePermissions(Resource = "Batch", Operation = "Write")]
        public ActionResult CreateBgaScrapBatch(int? id)
        {
            var scrapBatch = objects.GetOrCreateBgaScrapBatch(id, User);

            return View(scrapBatch);
        }

        [HttpPost]
        [AuthorizePermissions(Resource = "Batch", Operation = "Write")]
        public JsonResult AddBgaScrapBatchItem(int id, string sn)
        {
            var result = objects.AddBgaScrapBatchItem(id, sn, User);
            return Json(result);
        }

        [HttpPost]
        [AuthorizePermissions(Resource = "Batch", Operation = "Write")]
        public JsonResult DeleteBgaScrapBatchItem(int id)
        {
            return Json(objects.RemoveBgaScrapBatchItem(id));
        }

        [HttpGet]
        [AuthorizePermissions(Resource = "Batch", Operation = "Delete")]
        public ActionResult CancelBgaScrapBatch(int id)
        {
            return View(objects.GetOrCreateBgaScrapBatch(id, User));
        }

        [HttpPost]
        [AuthorizePermissions(Resource = "Batch", Operation = "Delete")]
        [ActionName("CancelBgaScrapBatch")]
        public ActionResult CancelBgaScrapBatchConfirmed(int id)
        {
            try
            {
                if (objects.PerformCancelBgaScrapBatch(id, User))
                {
                    return RedirectToActionPermanent("BgaScrapBatch");
                }

            }
            catch (Exception ex)
            {
                ViewBag.Message = String.Format("Cannot cancel this BGA batch. Information to support: {0}",
                    (ex.InnerException != null) ? ex.InnerException.Message : ex.Message);
            }

            return View(objects.GetOrCreateBgaScrapBatch(id, User));
        }

        [HttpGet]
        [AuthorizePermissions(Resource = "Batch", Operation = "Write")]
        public ActionResult ProcessBgaScrapBatch(int id)
        {
            var model = objects.GetOrCreateBgaScrapBatch(id, User);
            model.Delivered = DateTime.Now;
            return View(model);
        }

        [HttpPost]
        [AuthorizePermissions(Resource = "Batch", Operation = "Write")]
        [ValidateAntiForgeryToken]
        public ActionResult ProcessBgaScrapBatch(int id, BgaScrapBatch model)
        {
            BgaScrapBatch batch;
            try
            {
                batch = objects.PerformBgaScrapBatch(id, User, model.Delivered);
            }
            catch (InvalidOperationException ex)
            {
                ViewBag.Message = ex.Message;
                return View(model);
            }
            catch (Exception ex)
            {
                ViewBag.Message = String.Format("Oops, an error is caught. Please call the support " +
                    "with this information: {0}",
                    ex.InnerException != null ? ex.InnerException.Message : ex.Message);
                return View(model);
            }

            return RedirectToActionPermanent("BgaScrapBatch");
        }

        [HttpGet]
        [AuthorizePermissions(Resource = "Batch", Operation = "Write")]
        public CsvActionResult<ScrapBatchExport> BgaScrapBatchAsCsv(int id)
        {
            var batch = db.BgaScrapBatches.Find(id);
            var items = db.BgaScrapBatchItems
                .Where(d => d.BgaScrapBatchId == batch.Id)
                .Select(d => new { item = d, repair = db.Repairs.FirstOrDefault(r => r.RequestId == d.RefurbRequestId) })
                .ToList()
                .Select(d => new ScrapBatchExport
                {
                    TM = d.item.RefurbRequest.Model.TM,
                    ModelName = d.item.RefurbRequest.Model.Name,
                    ModelTypeName = d.item.RefurbRequest.Model.Type.Name,
                    SerialNumber = d.item.RefurbRequest.SerialNumber,
                    OriginId = d.item.RefurbRequest.WarehouseId,
                    Origin = (d.item.RefurbRequest.Warehouse != null)
                        ? d.item.RefurbRequest.Warehouse.Name
                        : string.Empty,
                    DestinationId = d.item.RefurbRequest.DestinationId,
                    Destination = (d.item.RefurbRequest.Destination != null)
                        ? d.item.RefurbRequest.Destination.Name
                        : string.Empty
                })
                .ToList();

            return new CsvActionResult<ScrapBatchExport>(items,
                string.Format("{0}.csv", batch.GetCode),
                ';');
        }
        #endregion
    }

    public class DeliveryBatchExport
    {
        public DateTime DateRequested { get; set; }
        public string SAPCode { get; set; }
        public string Model { get; set; }
        public string SerialNumber { get; set; }
        public DateTime? RepairedAt { get; set; }
        public string RepairedDescription { get; set; }
        public DateTime? BatchDeliveredDate { get; set; }
        public int Aging { get; set; }
        public string OriginId { get; set; }
        public string Origin { get; set; }
        public string DestinationId { get; set; }
        public string Destination { get; set; }
    }

    public class ScrapBatchExport
    {
        [DisplayName("Material")]
        public string TM { get; set; }

        [DisplayName("Denominação")]
        public string ModelName { get; set; }

        [DisplayName("Tipo")]
        public string ModelTypeName { get; set; }

        [DisplayName("Nº Série")]
        public string SerialNumber { get; set; }

        [DisplayName("Depósito Origem")]
        public string OriginId { get; set; }

        [DisplayName("Descrição Depósito Origem")]
        public string Origin { get; set; }

        [DisplayName("Depósito Destino")]
        public string DestinationId { get; set; }

        [DisplayName("Descrição Depósito Destino")]
        public string Destination { get; set; }
    }
}
