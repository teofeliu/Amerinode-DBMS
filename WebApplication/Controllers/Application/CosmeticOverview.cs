using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using WebApplication.Models.Application;
using WebApplication.Attributes;

namespace WebApplication.Controllers.Application
{
    [AuthorizePermissions(Resource = "CosmeticOverview")]
    public class CosmeticOverviewController : BaseAdminController<CosmeticOverview>
    {
        [AuthorizePermissions(Resource = "CosmeticOverview", Operation = "Write")]
        public override ActionResult Create(int? id)
        {
            return base.Create(id);
        }

        [AuthorizePermissions(Resource = "CosmeticOverview", Operation = "Write")]
        public override ActionResult Create(CosmeticOverview model, int? id, HttpPostedFileBase file)
        {
            return base.Create(model, id, file);
        }

        [AuthorizePermissions(Resource = "CosmeticOverview", Operation = "Delete")]
        public override ActionResult Delete(int? id)
        {
            return base.Delete(id);
        }

        [AuthorizePermissions(Resource = "CosmeticOverview", Operation = "Delete")]
        public override ActionResult DeleteConfirmed(int id)
        {
            return base.Delete(id);
        }
    }
}