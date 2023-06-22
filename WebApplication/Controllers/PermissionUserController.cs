using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using WebApplication.Attributes;
using WebApplication.Models.Application;
using WebApplication.Models.Auth;

namespace WebApplication.Controllers
{
    [AuthorizePermissions(Resource = "Permissions")]
    public class PermissionUserController : Controller
    {
        private DataModel _dbContext = new DataModel();
        private DataModel DbContext
        {
            get { return _dbContext; }
        }

        private ApplicationUserManager _userManager;
        private ApplicationUserManager userManager
        {
            get
            {
                return _userManager ?? HttpContext.GetOwinContext().Get<ApplicationUserManager>();
            }
        }

        // GET: PermissionUser
        [AuthorizePermissions(Resource = "Permissions", Operation = "Read")]
        public ActionResult Index(string id) // please, note that this ID is to USER object, not the PermissionUser key
        {
            ViewData["UserId"] = id;
            var user = userManager.FindById(id);
            var model = DbContext
                .PermissionsUser
                .Where(p => p.UserId == id)
                .OrderBy(p => p.Resource);

            return View(model);
        }

        [HttpGet]
        [AuthorizePermissions(Resource = "Permissions", Operation = "Write")]
        public ActionResult Create(string id)
        {
            // When editing roles, we need to list all resources and operations to assign permissions
            ViewData["ResourcesList"] = DbContext
                .PermissionResources
                .OrderBy(p => p.Name)
                .ToList();

            ViewData["OperationsList"] = DbContext
                .PermissionOperations
                .OrderBy(p => p.Name)
                .ToList();

            return View(new PermissionUser { UserId = id });
        }

        [HttpPost]
        [AuthorizePermissions(Resource = "Permissions", Operation = "Write")]
        public ActionResult Create(string id, PermissionUser model)
        {
            // when use a render action, the id is associated with model state, causing error on validation
            //  so, lets remove the validation form id field 
            ModelState.Remove("id");

            if (ModelState.IsValid)
            {
                try
                {
                    var perm = DbContext.Set<PermissionUser>().Add(model);
                    DbContext.SaveChanges();
                }
                catch (Exception e)
                {
                    // TODO: improve this error handler 
                    TempData["Errors"] = new[] { "This rule is already assined" };
                }
            }
            else
            {
                TempData["Errors"] = string.Join(" | ",
                ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage));
            }

            return RedirectToActionPermanent("Create", "User", new { Id = id });
        }

        [AuthorizePermissions(Resource = "Permissions", Operation = "Delete")]
        public ActionResult Delete(string id, int permId)
        {

            DbContext.Set<PermissionUser>()
                .Remove(DbContext.PermissionsUser.First(p => p.Id.Equals(permId)));
            DbContext.SaveChanges();

            return RedirectToActionPermanent("Create", "User", new { Id = id });
        }
    }
}