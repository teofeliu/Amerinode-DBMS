using System.Web.Mvc;
using WebApplication.Models.Application;

namespace WebApplication.Controllers.Application
{
    public interface IBatchOrdersController
    {
        ActionResult Create();
        ActionResult Create([Bind(Include = "Id,ModelId,Quantity,DateCreate,UserId")] BatchOrder batchOrder);
        ActionResult Index();
    }
}