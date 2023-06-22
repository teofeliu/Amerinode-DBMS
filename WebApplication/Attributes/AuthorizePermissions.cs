using Microsoft.AspNet.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Web;
using System.Web.Mvc;
using WebApplication.Models.Application;
using WebApplication.Models.Auth;

namespace WebApplication.Attributes
{
    public sealed class AuthorizePermissions : AuthorizeAttribute
    {
        private string _operation = "Read";

        public string Resource { get; set; }
        public string Operation
        {
            get { return _operation; }
            set { _operation = value; }
        }
        
        protected override bool AuthorizeCore(HttpContextBase httpContext)
        {
            var authorized = base.AuthorizeCore(httpContext);

            if (!authorized)
            {
                return authorized; // user do not have suficient privileges on generic resource
            }
            
            var user = httpContext.User;
            var userId = user.Identity.GetUserId();
            if (user.IsInRole("Administrator")) // the default role assigned on configuration Seed
            {
                return true;
            }

            using (var db = new DataModel())
            {


                // at first we need to check if user has a individual permission to execute the context action
                if (db.PermissionsUser.Any(p =>
                         p.Resource.Equals(Resource)
                         && p.Operation.Equals(Operation)
                         && p.UserId.Equals(userId)
                        ))
                {
                    return true;
                }
                // if user have no individual permissions, lets check if have any role with this context
                foreach (var perm in
                        db.PermissionsRoles
                        .Where(p => p.Resource.Equals(Resource)
                        && p.Operation.Equals(Operation)))
                {
                    if (user.IsInRole(perm.RoleName))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

    }

    public static partial class Helpers
    {
        public static bool HasPermissions(this IPrincipal user, string resource, string operation)
        {
            if(!user.Identity.IsAuthenticated)
            {
                return false;
            }

            var userId = user.Identity.GetUserId();

            if (user.IsInRole("Administrator")) // the default role assigned on configuration Seed
            {
                return true;
            }

            using (var db = new DataModel())
            {


                // at first we need to check if user has a individual permission to execute the context action
                if (db.PermissionsUser.Any(p =>
                         p.Resource.Equals(resource)
                         && p.Operation.Equals(operation)
                         && p.UserId.Equals(userId)
                        ))
                {
                    return true;
                }
                // if user have no individual permissions, lets check if have any role with this context
                foreach (var perm in
                        db.PermissionsRoles
                        .Where(p => p.Resource.Equals(resource)
                        && p.Operation.Equals(operation)))
                {
                    if (user.IsInRole(perm.RoleName))
                    {
                        return true;
                    }
                }
            }

            return false;
        }
        
    }
}