﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using WebApplication.Attributes;
using WebApplication.Models.Application;

namespace WebApplication.Controllers.Application
{
    [AuthorizePermissions(Resource = "RepairType")]
    public class RepairTypeController : BaseAdminController<RepairType>
    {
        [AuthorizePermissions(Resource = "RepairType", Operation = "Write")]
        public override ActionResult Create(int? id)
        {
            return base.Create(id);
        }

        [AuthorizePermissions(Resource = "RepairType", Operation = "Write")]
        public override ActionResult Create(RepairType model, int? id, HttpPostedFileBase file)
        {
            return base.Create(model, id, file);
        }

        [AuthorizePermissions(Resource = "RepairType", Operation = "Delete")]
        public override ActionResult Delete(int? id)
        {
            return base.Delete(id);
        }

        [AuthorizePermissions(Resource = "RepairType", Operation = "Delete")]
        public override ActionResult DeleteConfirmed(int id)
        {
            return base.DeleteConfirmed(id);
        }
    }
}