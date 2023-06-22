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
using System.Configuration;

namespace WebApplication.Controllers.Application
{
    [AuthorizePermissions(Resource = "Warranty")]
    public class WarrantyController : BaseAdminController<Warranty>
    {
        [AuthorizePermissions(Resource = "Warranty", Operation = "Write")]
        public override ActionResult Create(int? id)
        {
            return base.Create(id);
        }

        [AuthorizePermissions(Resource = "Warranty", Operation = "Write")]
        public override ActionResult Create(Warranty model, int? id, HttpPostedFileBase file)
        {
            return base.Create(model, id, file);
        }

        [AuthorizePermissions(Resource = "Warranty", Operation = "Delete")]
        public override ActionResult Delete(int? id)
        {
            return base.Delete(id);
        }

        [AuthorizePermissions(Resource = "Warranty", Operation = "Delete")]
        public override ActionResult DeleteConfirmed(int id)
        {
            return base.DeleteConfirmed(id);
        }
    }
}