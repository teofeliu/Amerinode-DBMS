using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin.Security;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using WebApplication.Attributes;
using WebApplication.Models;

namespace WebApplication.Controllers
{
    [AuthorizePermissions(Resource = "User", Operation = "Read")]
    public class UserController : Controller
    {
        ApplicationDbContext db = new ApplicationDbContext();

        private ApplicationSignInManager _signInManager;
        private ApplicationUserManager _userManager;

        public UserController()
        {
        }

        public UserController(ApplicationUserManager userManager, ApplicationSignInManager signInManager)
        {
            UserManager = userManager;
            SignInManager = signInManager;
        }

        public ApplicationSignInManager SignInManager
        {
            get
            {
                return _signInManager ?? HttpContext.GetOwinContext().Get<ApplicationSignInManager>();
            }
            private set
            {
                _signInManager = value;
            }
        }

        public ApplicationUserManager UserManager
        {
            get
            {
                return _userManager ?? HttpContext.GetOwinContext().GetUserManager<ApplicationUserManager>();
            }
            private set
            {
                _userManager = value;
            }
        }

        // GET: Auth
        [AuthorizePermissions(Resource = "User", Operation = "Read")]
        public ActionResult Index()
        {            
            //List<ApplicationUser> model = db.Set<ApplicationUser>().ToList();
            return View(db.Set<ApplicationUser>().ToList<ApplicationUser>());
        }

        [AuthorizePermissions(Resource = "User", Operation = "Write")]
        public virtual ActionResult Create(string id)
        {
            if (id != null)
            {
                RegisterViewModel model = new RegisterViewModel();
                var user = db.Users.Find(id);
                model.NameIdentifier = user.NameIdentifier;
                model.Email = user.Email;
                model.Id = user.Id;

                string roleId, roleName = "";
                if (user.Roles.Count() > 0)
                {
                    roleId = user.Roles.FirstOrDefault().RoleId;
                    roleName = db.Roles.SingleOrDefault(r => r.Id == roleId).Name;
                }

                ViewBag.Roles = new SelectList(db.Roles.ToList(), "Name", "Name", roleName);

                return View(model);
            }

            ViewBag.Roles = new SelectList(db.Roles.ToList(), "Name", "Name");

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthorizePermissions(Resource = "User", Operation = "Write")]
        public async Task<ActionResult> Create(RegisterViewModel model, string id)
        {
            if (id != null)
            {
                // Remove os campos de password da verificação do ModelState.IsValid
                ModelState.Remove("Password");
                ModelState.Remove("ConfirmPassword");

                if (ModelState.IsValid)
                {
                    ApplicationUser user = UserManager.FindById(model.Id);
                    user.NameIdentifier = model.NameIdentifier;
                    user.Email = model.Email;
                    user.UserName = model.Email;
                    
                    if(user.Roles.Count() == 1 && !string.IsNullOrEmpty(model.Role))
                    {
                        var roleId = user.Roles.FirstOrDefault().RoleId;
                        var roleName = db.Roles.SingleOrDefault(r => r.Id == roleId).Name;

                        UserManager.RemoveFromRole(user.Id, roleName);
                        UserManager.AddToRole(user.Id, model.Role);

                        //Atualizar cookies para mudança de role ter efeito
                        UserManager.UpdateSecurityStamp(user.Id);
                    }
                    else if(!string.IsNullOrEmpty(model.Role))
                    {
                        UserManager.AddToRole(user.Id, model.Role);
                    }

                    UserManager.Update(user);

                    return RedirectToAction("Index");
                }
            }

            else if (ModelState.IsValid)
            {
                var user = new ApplicationUser { UserName = model.Email, Email = model.Email, NameIdentifier = model.NameIdentifier };
                var result = await UserManager.CreateAsync(user, model.Password);
                if (result.Succeeded)
                {
                    await this.UserManager.AddToRoleAsync(user.Id, model.Role);
                    // For more information on how to enable account confirmation and password reset please visit http://go.microsoft.com/fwlink/?LinkID=320771
                    // Send an email with this link
                    // string code = await UserManager.GenerateEmailConfirmationTokenAsync(user.Id);
                    // var callbackUrl = Url.Action("ConfirmEmail", "Account", new { userId = user.Id, code = code }, protocol: Request.Url.Scheme);
                    // await UserManager.SendEmailAsync(user.Id, "Confirm your account", "Please confirm your account by clicking <a href=\"" + callbackUrl + "\">here</a>");

                    return RedirectToAction("Index");
                }
                AddErrors(result);
            }

            ViewBag.Roles = new SelectList(db.Roles.ToList(), "Name", "Name");
            // If we got this far, something failed, redisplay form
            return View(model);
        }

        [AuthorizePermissions(Resource = "User", Operation = "Write")]
        public ActionResult ChangePassword(string id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            var name = UserManager.FindById(id).NameIdentifier;
            AdminChangePasswordViewModel model = new AdminChangePasswordViewModel() { Id = id, NameIdentifier = name };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthorizePermissions(Resource = "User", Operation = "Write")]
        public async Task<ActionResult> ChangePassword(AdminChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            string resetToken = await UserManager.GeneratePasswordResetTokenAsync(model.Id);
            var passwordChangeResult = await UserManager.ResetPasswordAsync(model.Id, resetToken, model.ConfirmPassword);

            if (passwordChangeResult.Succeeded)
            {
                return RedirectToAction("Index");
            }

            //ViewBag.Error = "Password change failed.";
            AddErrors(passwordChangeResult);

            return View(model);
        }

        [AuthorizePermissions(Resource = "User", Operation = "Delete")]
        public virtual ActionResult Delete(string id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            var user = db.Set<ApplicationUser>().Find(id);
            if (user == null)
            {
                return HttpNotFound();
            }
            return View(user);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [AuthorizePermissions(Resource = "User", Operation = "Delete")]
        public virtual ActionResult DeleteConfirmed(string id)
        {
            var user = db.Set<ApplicationUser>().Find(id);
            db.Set<ApplicationUser>().Remove(user);
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        private void AddErrors(IdentityResult result)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error);
            }
        }
    }
}