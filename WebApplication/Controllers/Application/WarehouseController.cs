using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using WebApplication.Attributes;
using WebApplication.Models.Application;

namespace WebApplication.Controllers.Application
{
    [AuthorizePermissions(Resource = "Warehouse", Operation = "Read")]
    public class WarehouseController : Controller
    {
        [AuthorizePermissions(Resource = "Warehouse", Operation = "Read")]
        public ActionResult Index(string q)
        {
            List<Warehouse> warehouses = new List<Warehouse>();

            using (var db = new DataModel())
            {
                var data = db.Warehouses.AsQueryable();

                if (!String.IsNullOrEmpty(q))
                {
                    data = db.Warehouses
                        .Where(w => w.Id.Contains(q) || w.Name.Contains(q));
                }
                warehouses = data.ToList();
            }

            return View(warehouses);
        }

        [AuthorizePermissions(Resource = "Warehouse", Operation = "Read")]
        public ActionResult Create(string id)
        {
            ViewBag.WarehouseTypesList = Enum.GetValues(typeof(WarehouseType)).Cast<WarehouseType>();

            if (!String.IsNullOrEmpty(id))
            {
                Warehouse warehouse;
                using (var db = new DataModel())
                {
                    warehouse = db.Warehouses.Find(id);
                }
                return View(warehouse);
            }
            return View();
        }

        [HttpPost]
        [AuthorizePermissions(Resource = "Warehouse", Operation = "Write")]
        public ActionResult Create(string id, Warehouse model)
        {
            if (ModelState.IsValid)
            {
                using (var db = new DataModel())
                {
                    var warehouse = db.Warehouses.Find(id);
                    if (warehouse != null)
                    {
                        warehouse.Name = model.Name;
                        warehouse.WarehouseType = model.WarehouseType;
                    }
                    else
                    {
                        db.Warehouses.Add(model);
                    }
                    db.SaveChanges();
                }

                return RedirectToAction("Index");
            }

            return View(model);
        }
    }
}