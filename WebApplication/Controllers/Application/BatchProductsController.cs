using System;
using System.Data.Entity;
using System.Net;
using System.Web;
using System.Web.Mvc;
using WebApplication.Attributes;
using WebApplication.Models.Application;

namespace WebApplication.Controllers.Application
{
    [AuthorizePermissions(Resource = "Batch")]
    public class BatchProductsController : BaseAdminController<BatchProducts>
    {
        BatchManager objects = new BatchManager();
     
       public int id_url { get; set; }

        [AuthorizePermissions(Resource = "Batch", Operation = "Write")]
        public override ActionResult Create(int? id)
        {
            object selected = null;
            //ViewBag.Models = PopulateDropDown(typeof(Model), "Id", "Name", selected);
            ViewBag.Models = new SelectList(objects.GetModelIdFilterControlStock(), "Id", "Name");
            return View();
           
        }

        [AuthorizePermissions(Resource = "Batch", Operation = "Read")]
        public ActionResult _List()
        {
           
            int nRequest = Convert.ToInt32(RouteData.Values["id"]);
            id_url = nRequest;
            return View(objects.BatchProductsGet(nRequest));

        }

        [AuthorizePermissions(Resource = "Batch", Operation = "Read")]
        public ActionResult Details()
        {
            return View();
        }

            
        [HttpGet]
        [AuthorizePermissions(Resource = "Batch", Operation = "Read")]
        public ActionResult GetBatchProducts()
        {
            
            int nRequest = Convert.ToInt32(RouteData.Values["id"]);
            var list = objects.BatchProductsGet(nRequest);
            ViewBag.Dados = list;

            return Json(new { data = list }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        [AuthorizePermissions(Resource = "Batch", Operation = "Write")]
        public override ActionResult Create(BatchProducts model, int? id, HttpPostedFileBase file)
        {
            object selected = model.ModelId;
            //ViewBag.Models = PopulateDropDown(typeof(Model), "Id", "Name", selected);
            ViewBag.Models = new SelectList(objects.GetModelIdFilterControlStock(), "Id", "Name");

            model.DateCreate = DateTime.Now;
            model.BatchStockId = Convert.ToInt32(RouteData.Values["id"]);
            db.Set<BatchProducts>().Add(model);
            db.SaveChanges();
            
            //ADICIONANDO STOCK AO PRODUTO
            Model product = db.Set<Model>().Find(model.ModelId);
            product.Stock = product.Stock + model.Stock;
            db.Entry<Model>(product).State = EntityState.Modified;
            db.SaveChanges();


            return RedirectToAction("_List", "BatchProducts", new { id = model.BatchStockId });




        }

        [AuthorizePermissions(Resource = "Batch", Operation = "Read")]
        public override ActionResult Index()
        {
            return base.Index();
        }

        //public ActionResult _List()
        //{
        //    string nRequest = "2";
        //    return View(objects.BatchProductsGet(nRequest));

        //}
    }
}
