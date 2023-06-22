using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using WebApplication.Attributes;
using WebApplication.Models;
using WebApplication.Models.Application;

namespace WebApplication.Controllers
{
    [AuthorizePermissions(Resource = "Role", Operation = "Read")]
    public class RoleController : Controller
    {
        ApplicationDbContext db = new ApplicationDbContext();
        //RoleManager<IdentityRole> RoleManager = new RoleManager<IdentityRole>(new RoleStore<IdentityRole>(db));

        // GET: Role
        [AuthorizePermissions(Resource = "Role", Operation = "Read")]
        public ActionResult Index()
        {
            return View(db.Roles.ToList());
        }

        [AuthorizePermissions(Resource = "Role", Operation = "Write")]
        public virtual ActionResult Create(string id)
        {
            if (id != null)
            {
                return View(db.Roles.Find(id));
            }

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthorizePermissions(Resource = "Role", Operation = "Write")]
        public virtual ActionResult Create(IdentityRole model, string id)
        {
            if (id != null)
            {
                if (ModelState.IsValid)
                {
                    RoleManager<IdentityRole> RoleManager = new RoleManager<IdentityRole>(new RoleStore<IdentityRole>(db));
                    RoleManager.Update(model);

                    //return RedirectToAction("Index");
                    return RedirectToActionPermanent("Create", new { Id = id });
                }
            }

            else if (ModelState.IsValid)
            {
                var role = db.Roles.Add(model);
                db.SaveChanges();

                //return RedirectToAction("Index");
                return RedirectToActionPermanent("Create", new { Id = role.Id });
            }

            // If we got this far, something failed, redisplay form
            return View(model);
        }

        [AuthorizePermissions(Resource = "Role", Operation = "Delete")]
        public virtual ActionResult Delete(string id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            var role = db.Set<IdentityRole>().Find(id);
            if (role == null)
            {
                return HttpNotFound();
            }
            return View(role);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [AuthorizePermissions(Resource = "Role", Operation = "Delete")]
        public virtual ActionResult DeleteConfirmed(string id)
        {
            var role = db.Set<IdentityRole>().Find(id);
            db.Set<IdentityRole>().Remove(role);
            db.SaveChanges();
            return RedirectToAction("Index");
        }


        [AuthorizePermissions(Resource = "Role", Operation = "Read")]
        public ActionResult UsersInRole(string id)
        {
            ViewData["RoleID"] = id;

            var users = new List<ApplicationUser>();
            using (var appDbContext = new ApplicationDbContext())
            {
                users.AddRange(appDbContext
                    .Set<ApplicationUser>()
                    .Where(u => u.Roles.Any(r => r.RoleId == id))
                    .ToList()
                    );
            }

            return View(users);
        }

        [AuthorizePermissions(Resource = "Role", Operation = "Write")]
        public JsonResult RemoveUser(string roleId, string userId)
        {
            using (var context = new ApplicationDbContext())
            {
                var role = context.Roles.FirstOrDefault(r => r.Id == roleId);

                var userStore = new UserStore<ApplicationUser>(context);
                var userManager = new UserManager<ApplicationUser>(userStore);

                userManager.RemoveFromRole(userId, role.Name);
            }

                return Json(new { });
        }
    }
}