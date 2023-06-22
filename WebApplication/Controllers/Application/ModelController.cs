using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Data.Entity;
using WebApplication.Models.Application;
using System.IO;
using System.Configuration;
using WebApplication.Attributes;

namespace WebApplication.Controllers.Application
{
    [AuthorizePermissions(Resource = "Model")]
    public class ModelController : BaseAdminController<Model>
    {
        [AuthorizePermissions(Resource = "Model", Operation = "Write")]
        public override ActionResult Create(int? id)
        {
            object selected = null;
            object selectedModel = null;
            var model = new Model();
            if (id != null)
            {
                model = db.Set<Model>().Find(id);
                selected = model.ManufacturerId;
                selectedModel = model.ModelTypeId;
            }
            ViewBag.Manufacturers = PopulateDropDown(typeof(Manufacturer), "Id", "Name", selected);
            ViewBag.ModelTypes = PopulateDropDown(typeof(ModelType), "Id", "Name", selectedModel);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthorizePermissions(Resource = "Model", Operation = "Write")]
        public override ActionResult Create(Model model, int? id, HttpPostedFileBase file)
        {
            object selected = model.ManufacturerId;
            object selectedModel = model.ModelTypeId;
            ViewBag.Manufacturers = PopulateDropDown(typeof(Manufacturer), "Id", "Name", selected);
            ViewBag.ModelTypes = PopulateDropDown(typeof(ModelType), "Id", "Name", selectedModel);
            if (ModelState.IsValid)
            {
                if (id == null)
                {
                    // Create
                    db.Set<Model>().Add(model);
                }
                else{
                    db.Entry(model).State = EntityState.Modified;
                }

                db.SaveChanges();

                return RedirectToAction("Index");
            }
            
            ViewBag.Message = "Please, check your entries.";
            return View(model);
        }

        [AuthorizePermissions(Resource = "Model", Operation = "Delete")]
        public override ActionResult Delete(int? id)
        {
            return base.Delete(id);
        }

        [AuthorizePermissions(Resource = "Model", Operation = "Delete")]
        public override ActionResult DeleteConfirmed(int id)
        {
            return base.DeleteConfirmed(id);
        }
    }
}