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
    [AuthorizePermissions(Resource = "Supply")]
    public class SupplyController : BaseAdminController<Supply> 
    {
        [AuthorizePermissions(Resource = "Supply", Operation = "Write")]
        public override ActionResult Create(int? id)
        {
            return base.Create(id);
        }

        [AuthorizePermissions(Resource = "Supply", Operation = "Write")]
        public override ActionResult Create(Supply model, int? id, HttpPostedFileBase file)
        {
            return base.Create(model, id, file);
        }

        [AuthorizePermissions(Resource = "Supply", Operation = "Delete")]
        public override ActionResult Delete(int? id)
        {
            return base.Delete(id);
        }

        [AuthorizePermissions(Resource = "Supply", Operation = "Delete")]
        public override ActionResult DeleteConfirmed(int id)
        {
            return base.DeleteConfirmed(id);
        }
    }
}