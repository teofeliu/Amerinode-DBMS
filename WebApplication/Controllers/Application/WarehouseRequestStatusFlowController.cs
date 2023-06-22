using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Helpers;
using System.Web.Mvc;
using WebApplication.Attributes;
using WebApplication.Models.Application;

namespace WebApplication.Controllers.Application
{
    [AuthorizePermissions(Resource = "WarehouseRequestStatusFlow", Operation = "Write")]
    public class WarehouseRequestStatusFlowController : Controller
    {
        public ActionResult Index()
        {
            var model = new List<RequestFlowStatusWarehouse>();

            using (var db = new DataModel())
            {
                var enums = Enum
                    .GetValues(typeof(RequestFlowStatus))
                    .Cast<RequestFlowStatus>()
                    .ToList();

                var warehouseStatus = db.WarehouseRequestStatuses.ToList();

                var options = enums
                    .GroupJoin(warehouseStatus,
                    e => e,
                    w => w.RequestFlowStatus,
                    (e, w) => new
                    {
                        status = e,
                        warehouse = w
                    })
                    .ToList();

                var warehouses = db.Warehouses.ToList();

                foreach (var item in options)
                {
                    var s = new RequestFlowStatusWarehouse
                    {
                        Status = item.status,
                        Items = new List<SelectListItem>()
                    };
                    s.Status = item.status;
                    s.Items.Add(new SelectListItem
                    {
                        Value = string.Empty,
                        Text = "None",
                        Selected = true
                    });
                    foreach (var w in warehouses)
                    {
                        s.Items.Add(new SelectListItem
                        {
                            Value = w.Id,
                            Text = w.ToString(),
                            Selected = item.warehouse
                            .Any(x => x.WarehouseId == w.Id
                            && x.RequestFlowStatus == item.status)
                        });
                    }
                    model.Add(s);
                }

            }

            return View(model);
        }

        [HttpPost]
        public JsonResult Save(RequestFlowStatus status, string warehouseId)
        {
            var message = string.Empty;
            try
            {
                using (var db = new DataModel())
                {
                    var warehouseFlow = db.WarehouseRequestStatuses
                        .FirstOrDefault(wfs => wfs.RequestFlowStatus == status);
                    if (warehouseFlow == null)
                    {
                        if(!String.IsNullOrEmpty(warehouseId))
                        {
                            db.WarehouseRequestStatuses.Add(new WarehouseRequestStatus
                            {
                                RequestFlowStatus = status, 
                                WarehouseId = warehouseId
                            });
                            db.SaveChanges();
                        }
                    }
                    else
                    {
                        if (string.IsNullOrEmpty(warehouseId))
                        {
                            db.WarehouseRequestStatuses.Remove(warehouseFlow);
                        }
                        else
                        {
                            warehouseFlow.WarehouseId = warehouseId;
                            db.SaveChanges();
                        }
                    }
                }
            }
            catch (Exception e)
            {
                message = (e.InnerException != null) ? e.InnerException.Message : e.Message;
            }
            return Json(new { message });
        }
    }

    public class RequestFlowStatusWarehouse
    {
        public RequestFlowStatus Status { get; set; }
        public List<SelectListItem> Items { get; set; }
    }
}