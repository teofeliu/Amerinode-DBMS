using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using System;
using System.Collections.Generic;
using System.Data.Entity.Core;
using System.Data.SqlClient;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using WebApplication.Attributes;
using WebApplication.Models;
using WebApplication.Models.Application;
using WebApplication.Models.Auth;

namespace WebApplication.Controllers
{
    [AuthorizePermissions(Resource = "Permissions")]
    public class PermissionRoleController : Controller
    {
        private DataModel _dbContext = new DataModel();
        private RoleManager<IdentityRole> _roleManager = new RoleManager<IdentityRole>(new RoleStore<IdentityRole>(new ApplicationDbContext()));

        private DataModel dbContext
        {
            get { return _dbContext; }
        }
        private RoleManager<IdentityRole> roleManager
        {
            get { return _roleManager; }
        }

        // GET: PermissionRole
        [AuthorizePermissions(Resource = "Permissions", Operation = "Read")]
        public ActionResult Index(string id)
        {
            ViewData["RoleId"] = id;
            var role = roleManager.FindById(id);
            var model = dbContext
                .PermissionsRoles
                .Where(p => p.RoleName.Equals(role.Name))
                .OrderBy(p => p.Resource);

            return View(model);
        }


        [HttpGet]
        [AuthorizePermissions(Resource = "Permissions", Operation = "Write")]
        public ActionResult Create(string id)
        {
            var role = roleManager.FindById(id);

            var resources = dbContext
                .PermissionResources
                .OrderBy(r => r.Name)
                .ToList();

            var operations = dbContext
                .PermissionOperations
                .ToList();

            var rules = dbContext
                .PermissionsRoles
                .Where(pr => pr.RoleName == role.Name)
                .ToList();

            var model = new List<PermissionRoleViewModel>();
            foreach (var resource in resources)
            {
                foreach (var operation in operations)
                {
                    model.Add(new PermissionRoleViewModel
                    {
                        RoleName = role.Name,
                        Resource = resource.Name,
                        Operation = operation.Name,
                        IsSelected = rules.Any(r => r.Resource == resource.Name
                        && r.Operation == operation.Name)
                    });
                }
            }

            return View(model);
        }

        [HttpPost]
        [AuthorizePermissions(Resource = "Permissions", Operation = "Write")]
        public JsonResult Create(PermissionRoleViewModel model)
        {
            var permission = dbContext
                .PermissionsRoles
                .FirstOrDefault(pr => 
                    pr.RoleName == model.RoleName
                    && pr.Resource == model.Resource
                    && pr.Operation == model.Operation
                );
            if(permission == null && model.IsSelected)
            {
                dbContext.PermissionsRoles.Add(new PermissionRole
                {
                    RoleName = model.RoleName,
                    Resource = model.Resource,
                    Operation = model.Operation
                });
            }
            if(permission != null && !model.IsSelected)
            {
                dbContext.PermissionsRoles.Remove(permission);
            }
            dbContext.SaveChanges();


            return Json(model);
        }


        [AuthorizePermissions(Resource = "Permissions", Operation = "Delete")]
        public ActionResult Delete(string id, int permId)
        {

            dbContext.Set<PermissionRole>()
                .Remove(dbContext.PermissionsRoles.First(p => p.Id.Equals(permId)));
            dbContext.SaveChanges();

            return RedirectToActionPermanent("Create", "Role", new { Id = id });
        }
    }
}