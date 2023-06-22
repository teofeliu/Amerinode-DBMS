using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using WebApplication.Attributes;
using WebApplication.Models.Application;

namespace WebApplication.Controllers.Application
{
    [AuthorizePermissions(Resource = "HardwareOverview")]
    public class HardwareOverviewController : BaseAdminController<HardwareOverview>
    {
        [AuthorizePermissions(Resource = "HardwareOverview", Operation = "Write")]
        public override ActionResult Create(HardwareOverview model, int? id, HttpPostedFileBase file)
        {
            return base.Create(model, id, file);
        }

        [AuthorizePermissions(Resource = "HardwareOverview", Operation = "Write")]
        public override ActionResult Create(int? id)
        {
            return base.Create(id);
        }

        [AuthorizePermissions(Resource = "HardwareOverview", Operation = "Delete")]
        public override ActionResult Delete(int? id)
        {
            return base.Delete(id);
        }

        [AuthorizePermissions(Resource = "HardwareOverview", Operation = "Delete")]
        public override ActionResult DeleteConfirmed(int id)
        {
            return base.DeleteConfirmed(id);
        }
    }
}