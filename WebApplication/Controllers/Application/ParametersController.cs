using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using WebApplication.Attributes;
using WebApplication.Models.Application;

namespace WebApplication.Controllers.Application
{
    [AuthorizePermissions(Resource = "Parameters")]
    public class ParametersController : BaseAdminController<Parameters>
    {
        [AuthorizePermissions(Resource = "Parameters", Operation = "Write")]
        public override ActionResult Index()
        {
            return base.Index();
        }
    }
}