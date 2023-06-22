using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using WebApplication.Attributes;
using WebApplication.Models.Application;

namespace WebApplication.Controllers.Application
{
    [AuthorizePermissions(Resource = "Batch")]
    public class BatchOrdersController : Controller
    {
        private DataModel db = new DataModel();
        BatchManager objects = new BatchManager();

        [AuthorizePermissions(Resource = "Batch", Operation = "Read")]
        public ActionResult Index()
        {
            var batchOrders = db.BatchOrders.Include(b => b.Model);
            return View(batchOrders.ToList());
        }

        [AuthorizePermissions(Resource = "Batch", Operation = "Write")]
        public ActionResult Create()
        {
            //ViewBag.ModelId = new SelectList(db.Models, "Id", "Name");
            ViewBag.ModelId = new SelectList(objects.GetModelIdFilterStock(), "Id", "Name");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthorizePermissions(Resource = "Batch", Operation = "Write")]
        public ActionResult Create([Bind(Include = "Id,ModelId,Quantity,DateCreate,UserId")] BatchOrder batchOrder)
        {
                   

            //Controlando o Estoque

                Model model =  db.Models.Find(batchOrder.ModelId);
                int quantidade = model.Stock - batchOrder.Quantity;

            if(quantidade < 0)
            {
                ViewBag.Message = "Insufficient stock for this product!";
               
                ViewBag.ModelId = new SelectList(objects.GetModelIdFilterStock(), "Id", "Name");
                return View();
            }
            else
            {
                batchOrder.UserId = User.Identity.Name;
                batchOrder.DateCreate = DateTime.Now;
                db.BatchOrders.Add(batchOrder);
                db.SaveChanges();

                model.Stock = model.Stock - batchOrder.Quantity;
                db.SaveChanges();
            }
                return RedirectToAction("Index");            
        }

       
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
