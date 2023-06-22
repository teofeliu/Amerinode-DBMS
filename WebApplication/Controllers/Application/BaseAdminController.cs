using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Data;
using System.Data.Entity;
using WebApplication.Models.Application;
using System.IO;
using System.Linq.Dynamic;
using WebApplication.Attributes;
using System.Net;

namespace WebApplication.Controllers.Application
{
    public abstract class BaseAdminController<T> : Controller where T : class
    {
        DataModel _db = null;

        protected DataModel db
        {
            get
            {
                if (_db == null)
                    _db = new DataModel();
                return _db;
            }
        }



        protected SelectList PopulateDropDown(Type model, string Id, string DisplayField, object selected = null)
        {
            return new SelectList(this.db.Set(model), Id, DisplayField, selected);
        }


        protected string SaveTo {
            get
            {
                var path = Server.MapPath("/Uploads");
                if (!System.IO.Directory.Exists(path))
                    System.IO.Directory.CreateDirectory(path);
                return path;
            }
        }

        protected List<string> GetFilterFields()
        {
            List<string> attrs = new List<string>();
            foreach (var property in typeof(T).GetProperties())
            {
                foreach (var attr in property.GetCustomAttributes(true)){
                    if (attr is FilterField)
                    {
                        attrs.Add(property.Name);
                    }
                }
            }
            return attrs;
        }

        public virtual ActionResult Index()
        {
            var attrs = GetFilterFields();
            if (Request.QueryString.Count > 0)
            {
                List<string> where = new List<string>();
                object[] param = new object[attrs.Count];
                for (var i = 0; i < attrs.Count; i++)
                {
                    where.Add(attrs[i] + ".Contains(@" + i + ")");
                    param[i] = Request.QueryString["q"];
                }
                List<T> results = db.Set<T>().AsQueryable().Where(String.Join(" || ", where), param).ToList<T>();

                return View(results);
            }
            return View(db.Set<T>().ToList<T>());
        }


        public virtual ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            T entity = db.Set<T>().Find(id);
            if (entity == null)
            {
                return HttpNotFound();
            }
            return View(entity);
        }


        protected virtual void DeleteImage(string img){
            System.IO.File.Delete(img);
        }


        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public virtual ActionResult DeleteConfirmed(int id)
        {
            T entity = db.Set<T>().Find(id);
            db.Set<T>().Remove(entity);
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        protected virtual bool IsImage(HttpPostedFileBase file)
        {
            var allowedExtensions = new[] { ".png", ".jpg", "jpeg" };
            var ext = Path.GetExtension(file.FileName).ToLower();

            if (allowedExtensions.Contains(ext)){
                return true;
            }
            else return false;
        }

        protected virtual string RemoveWhitespace(string str)
        {
            return string.Join("_", str.Split(default(string[]), StringSplitOptions.RemoveEmptyEntries));
        }

        protected virtual string GetCurrentImage(int? id)
        {
            return "";
        }

        protected override void Dispose(bool disposing)
        {
            db.Dispose();
            base.Dispose(disposing);
        }

        public virtual ActionResult Create(int? id)
        {
            if (id != null)
            {
                T model = db.Set<T>().Find(id);
                return View(model);
            }
            
            return View();    
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public virtual ActionResult Create(T model, int? id, HttpPostedFileBase file)
        {
            if (ModelState.IsValid)
            {
                    if (id == null){
                        // Create
                        db.Set<T>().Add(model);
                    }
                    else
                    {
                     db.Entry(model).State = EntityState.Modified;
                    }
                    db.SaveChanges();
                return RedirectToAction("Index");
            }

            ViewBag.Message = "Please, check your entries.";
            return View(model);
        }
    }
}