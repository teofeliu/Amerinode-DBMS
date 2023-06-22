using System.Linq;
using System.Web.Mvc;
using System.Data.Entity;
using WebApplication.Models.Application;

namespace WebApplication.Controllers.Application
{
    public class ProcessController : Controller
    {
        DataModel db = new DataModel();
        public ActionResult Index()
        {
            return View(db.RequestFlows.AsNoTracking().Single());
        }

        [HttpPost]
        public ActionResult Index(Models.Application.RequestFlow model)
        {
            if(ModelState.IsValid)
            {
                db.Entry(model).State = EntityState.Modified;
                db.SaveChanges();

                return View("Index");
            }

            ViewBag.Message = "Error validating a (some) field(s).";
            
            return View(model);
        }
    }
}