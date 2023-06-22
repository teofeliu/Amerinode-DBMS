using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using WebApplication.Attributes;
using WebApplication.Models.Application;
using System.IO;
using DataTables.AspNet.Core;
using DataTables.AspNet.Mvc5;
using WebApplication.Extensions;
using System.Reflection;
using System.Linq.Dynamic;
using System.Text;

namespace WebApplication.Controllers.Application
{
    [AuthorizePermissions(Resource = "DOA")]
    public class DOAController : BaseAdminController<DOA>
    {
        DOAManager doam = new DOAManager();

        [AuthorizePermissions(Resource = "DOA", Operation = "Read")]
        public JsonResult PageData(IDataTablesRequest request)
        {
            // card https://trello.com/c/15ZOC6Hz
            // do not restrict search to status
            IQueryable<RefurbRequest> data = db.RefurbRequests.Where(rr => rr.isDOA);
            if (String.IsNullOrEmpty(request.Search.Value))
            {
                data = data.Where(rr => !rr.Cancelled);
            }

            //if (String.IsNullOrEmpty(request.Search.Value))
            //{
            //    var statusNotAllowed = new[] { RequestFlowStatus.FinalInspection, RequestFlowStatus.SentToScrap };
            //    // statement implemented by card https://trello.com/c/7VW0MhUa
            //    if (status != null && status == RequestFlowStatus.SentToScrap)
            //    {
            //        statusNotAllowed = new[] { RequestFlowStatus.FinalInspection };
            //    }
            //    // eo statement implemented by card https://trello.com/c/7VW0MhUa

            //    data = db.RefurbRequests.Where(rr => !statusNotAllowed.Contains(rr.Status));
            //}
            //else {
            //    data = data.Where(rr => !rr.Cancelled);
            //}

            // eo card https://trello.com/c/15ZOC6Hz

            var filteredData = data;

            var attrs = GetFilterFields();
            if (!String.IsNullOrEmpty(request.Search.Value))
            {
                List<string> where = new List<string>();
                object[] param = new object[attrs.Count];
                Type t = new RefurbRequest().GetType();

                for (var i = 0; i < attrs.Count; i++)
                {
                    PropertyInfo prop = t.GetProperty(attrs[i]);

                    if (prop.PropertyType == typeof(string))
                    {
                        where.Add(attrs[i] + ".Contains(@" + i + ")");
                        param[i] = request.Search.Value;
                    }
                    if (prop.PropertyType == typeof(int) || prop.PropertyType == typeof(int?))
                    {
                        int val;
                        if (int.TryParse(request.Search.Value, out val))
                        {
                            where.Add(attrs[i] + " = @" + i);
                            param[i] = val;
                        }
                    }
                    if (prop.PropertyType == typeof(RequestFlowStatus))
                    {
                        RequestFlowStatus val;
                        if (Enum.TryParse<RequestFlowStatus>(request.Search.Value, true, out val))
                        {
                            where.Add(attrs[i] + " = @" + i + "");
                            param[i] = (RequestFlowStatus)val;
                        }
                    }
                }

                filteredData = filteredData.Where(x => !x.Cancelled).Where(String.Join(" || ", where), param);
            }

            // card https://trello.com/c/QZzvlT2Z
            // sorting features enabled
            var colSorting = request.Columns.FirstOrDefault(c => c.Sort != null);
            if (colSorting == null)
            {
                filteredData = filteredData.OrderBy(f => f.Id);
            }
            else
            {
                switch (colSorting.Field)
                {
                    case "serialNumber":
                        if (colSorting.Sort.Direction == SortDirection.Ascending)
                            filteredData = filteredData.OrderBy(f => f.SerialNumber);
                        else
                            filteredData = filteredData.OrderByDescending(f => f.SerialNumber);
                        break;
                    case "dateRequested":
                        if (colSorting.Sort.Direction == SortDirection.Ascending)
                            filteredData = filteredData.OrderBy(f => f.DateRequested);
                        else
                            filteredData = filteredData.OrderByDescending(f => f.DateRequested);
                        break;
                    case "lastUpdated":
                        if (colSorting.Sort.Direction == SortDirection.Ascending)
                            filteredData = filteredData.OrderBy(f => f.LastUpdated);
                        else
                            filteredData = filteredData.OrderByDescending(f => f.LastUpdated);
                        break;
                    default:
                        if (colSorting.Sort.Direction == SortDirection.Ascending)
                            filteredData = filteredData.OrderBy(f => f.Id);
                        else
                            filteredData = filteredData.OrderByDescending(f => f.Id);
                        break;
                }
            }
            // eo // card https://trello.com/c/QZzvlT2Z

            var dataPage = filteredData
                .Select(d => new
                {
                    d.Id,
                    d.Model.PartNumber,
                    d.SerialNumber,
                    d.UserId,
                    d.DateRequested,
                    d.LastUpdated,
                    d.Status
                })
                //.OrderBy(d => d.Id)
                .Skip(request.Start)
                .Take(request.Length)
                .ToList()
                .Select(d => new
                {
                    d.Id,
                    d.PartNumber,
                    d.SerialNumber,
                    d.UserId,
                    d.DateRequested,
                    d.LastUpdated,
                    statusDescription = Utility.GetDescriptionFromEnumValue(d.Status),
                    d.Status
                });

            var response = DataTablesResponse.Create(request, data.Count(), filteredData.Count(), dataPage);

            return new DataTablesJsonResult(response);
        }

        [AuthorizePermissions(Resource = "DOA", Operation = "Write")]
        public ActionResult Import()
        {
            return View();
        }

        [HttpPost]
        [AuthorizePermissions(Resource = "DOA", Operation = "Write")]
        public ActionResult Import(HttpPostedFileBase File)
        {
            if (!validateFile(File)) return View();

            try
            {
                doam.CreateDOAFromExcelStream(File.InputStream, Path.GetExtension(File.FileName));
            }
            catch (Exception ex)
            {
                ViewBag.Message = (ex.InnerException != null)
                    ? ex.InnerException.Message
                    : ex.Message;
                return View();
            }

            return RedirectToAction("Index", "DOA");
        }

        [AuthorizePermissions(Resource = "DOA", Operation = "Write")]
        public CsvActionResult<RefurbRequestExportModel> ExportAllAsCsv()
        {
            var query = new StringBuilder();
            query.Append(" SELECT  ");
            query.Append(" 	[rr].[Id] AS [RequestId],  ");
            query.Append(" 	[rr].[ModelId],  ");
            query.Append(" 	[m].[Name] AS [Model],  ");
            query.Append(" 	[m].[TM] AS [SAPCode],  ");
            query.Append(" 	[m].[PartNumber],  ");
            query.Append(" 	[m].[ModelTypeId],  ");
            query.Append(" 	[mt].[Name] AS [ModelType],  ");
            query.Append(" 	[m].[ManufacturerId],  ");
            query.Append(" 	[ma].[Name] AS [Manufacturer],  ");
            query.Append(" 	[rr].[SerialNumber],  ");
            query.Append(" 	[rr].[DateRequested],  ");
            query.Append("  [rr].[IsDOA],  ");
            query.Append(" 	CASE [rr].[Status]  ");
            query.Append(" 		WHEN 0 THEN 'Received'  ");
            query.Append(" 		WHEN 1 THEN 'TrialPerformed'  ");
            query.Append(" 		WHEN 2 THEN 'SentToRepair'  ");
            query.Append(" 		WHEN 3 THEN 'Repaired'  ");
            query.Append(" 		WHEN 4 THEN 'SentToCosmetic'  ");
            query.Append(" 		WHEN 5 THEN 'CosmeticPerformed'  ");
            query.Append(" 		WHEN 6 THEN 'SentToScrapEvaluation'  ");
            query.Append(" 		WHEN 7 THEN 'SentToScrap'  ");
            query.Append(" 		WHEN 8 THEN 'SentToDOA'  ");
            query.Append(" 		WHEN 9 THEN 'SentToFinalInspection'  ");
            query.Append(" 		WHEN 10 THEN 'FinalInspection'  ");
            query.Append(" 		WHEN 11 THEN 'SentToCosmeticHold'  ");
            query.Append(" 		WHEN 12 THEN 'SentBackToTrial'  ");
            query.Append(" 		WHEN 13 THEN 'Delivered'  ");
            query.Append(" 		ELSE 'N/A'  ");
            query.Append(" 	END AS [Status],  ");
            query.Append(" 	[rr].[BatchId],  ");
            query.Append(" 	MAX(CASE WHEN [rf].[Status] = 0 THEN [rf].[DateRequested] END) AS [ReceivedAt],  ");
            query.Append(" 	[wo].[Id] + ' - ' + [wo].[Name] AS [Origin], ");
            query.Append(" 	MAX(CASE WHEN [rf].[Status] = 1 THEN [rf].[DateRequested] END) AS [TrialPerformedAt],  ");
            query.Append(" 	[ww].[Id] + ' - ' + [ww].[Name] AS [Warehouse], ");
            query.Append(" 	[rr].[DateWarehouse], ");
            query.Append(" 	MAX(CASE WHEN [rf].[Status] = 2 THEN [rf].[DateRequested] END) AS [SentToRepairAt],  ");
            query.Append(" 	MAX(CASE WHEN [rf].[Status] = 3 THEN [rf].[DateRequested] END) AS [RepairedAt],  ");
            // query.Append(" 	[re].[Description] AS [RepairDescription],  ");
            query.Append(" 	[RepairDescription] = STUFF((SELECT '; ' + [Description] FROM[dbo].[Repairs] WHERE[RequestId] = [rr].[Id] FOR XML PATH(''), type).value('.', 'nvarchar(max)'), 1, 2, ''),  ");
            query.Append(" 	MAX(CASE WHEN [rf].[Status] = 4 THEN [rf].[DateRequested] END) AS [SentToCosmeticAt],  ");
            query.Append(" 	MAX(CASE WHEN [rf].[Status] = 5 THEN [rf].[DateRequested] END) AS [CosmeticPerformedAt],  ");
            query.Append(" 	MAX(CASE WHEN [rf].[Status] = 6 THEN [rf].[DateRequested] END) AS [SentToScrapEvaluationAt],  ");
            query.Append(" 	MAX(CASE WHEN [rf].[Status] = 7 THEN [rf].[DateRequested] END) AS [SentToScrapAt],  ");
            query.Append(" 	MAX(CASE WHEN [rf].[Status] = 8 THEN [rf].[DateRequested] END) AS [SentToDOAAt],  ");
            query.Append(" 	MAX(CASE WHEN [rf].[Status] = 9 THEN [rf].[DateRequested] END) AS [SentToFinalInspectionAt],  ");
            query.Append(" 	MAX(CASE WHEN [rf].[Status] = 10 THEN [rf].[DateRequested] END) AS [FinalInspectionAt],  ");
            query.Append(" 	MAX(CASE WHEN [rf].[Status] = 11 THEN [rf].[DateRequested] END) AS [SentToCosmeticHoldAt],  ");
            query.Append(" 	MAX(CASE WHEN [rf].[Status] = 12 THEN [rf].[DateRequested] END) AS [SentBackToTrialAt],  ");
            query.Append(" 	MAX(CASE WHEN ([rf].[Status] = 13 OR [rf].[Status] = 7) THEN [rf].[DateRequested] END) AS [Delivered], ");
            query.Append(" 	[wd].[Id] + ' - ' + [wd].[Name] AS [Destination] ");
            query.Append(" FROM [dbo].[RequestFlows] AS [rf]  ");
            query.Append(" INNER JOIN [dbo].[RefurbRequests] AS [rr]  ");
            query.Append(" 	ON [rf].[RequestId] = [rr].[Id]  ");
            query.Append(" INNER JOIN [dbo].[Models] AS [m]  ");
            query.Append(" 	ON [rr].[ModelId] = [m].[Id]  ");
            query.Append(" INNER JOIN [dbo].[ModelTypes] AS [mt]  ");
            query.Append(" 	ON [m].[ModelTypeId] = [mt].[Id]  ");
            query.Append(" LEFT JOIN [dbo].[Manufacturers] AS [ma]  ");
            query.Append(" 	ON [m].[ManufacturerId] = [ma].[Id]   ");
            //query.Append(" LEFT JOIN [dbo].[Repairs] AS [re]  ");
            //query.Append(" 	ON [rr].[Id] = [re].RequestId ");
            query.Append(" LEFT JOIN [dbo].[Warehouses] AS [wo] ");
            query.Append(" 	ON CASE [rr].[OriginId] ");
            query.Append(" 		WHEN '84' THEN '88' ");
            query.Append(" 		WHEN '83' THEN '87' ");
            query.Append(" 		ELSE [rr].[OriginId] END = [wo].[Id] ");
            query.Append(" LEFT JOIN [dbo].[Warehouses] AS [ww] ");
            query.Append(" 	ON [rr].[WarehouseId] = [ww].[Id] ");
            query.Append(" LEFT JOIN [dbo].[Warehouses] AS [wd] ");
            query.Append(" 	ON [rr].[DestinationId] = [wd].[Id] ");
            query.Append(" WHERE [rr].[Cancelled] = 0 ");
            query.Append(" GROUP BY [rr].[Id],  ");
            query.Append(" 	[rr].[ModelId],  ");
            query.Append(" 	[m].[Name],  ");
            query.Append(" 	[m].[PartNumber],  ");
            query.Append(" 	[m].[TM],  ");
            query.Append(" 	[m].[ModelTypeId],  ");
            query.Append(" 	[mt].[Name],  ");
            query.Append(" 	[m].[ManufacturerId],  ");
            query.Append(" 	[ma].[Name],  ");
            query.Append(" 	[rr].[SerialNumber],  ");
            query.Append("  [rr].[IsDOA],  ");
            query.Append(" 	[rr].[DateRequested],  ");
            query.Append(" 	CASE [rr].[Status]  ");
            query.Append(" 		WHEN 0 THEN 'Received'  ");
            query.Append(" 		WHEN 1 THEN 'TrialPerformed'  ");
            query.Append(" 		WHEN 2 THEN 'SentToRepair'  ");
            query.Append(" 		WHEN 3 THEN 'Repaired'  ");
            query.Append(" 		WHEN 4 THEN 'SentToCosmetic'  ");
            query.Append(" 		WHEN 5 THEN 'CosmeticPerformed'  ");
            query.Append(" 		WHEN 6 THEN 'SentToScrapEvaluation'  ");
            query.Append(" 		WHEN 7 THEN 'SentToScrap'  ");
            query.Append(" 		WHEN 8 THEN 'SentToDOA'  ");
            query.Append(" 		WHEN 9 THEN 'SentToFinalInspection'  ");
            query.Append(" 		WHEN 10 THEN 'FinalInspection'  ");
            query.Append(" 		WHEN 11 THEN 'SentToCosmeticHold'  ");
            query.Append(" 		WHEN 12 THEN 'SentBackToTrial'  ");
            query.Append(" 		WHEN 13 THEN 'Delivered'  ");
            query.Append(" 		ELSE 'N/A'  ");
            query.Append(" 	END,  ");
            query.Append(" 	[rr].[BatchId], ");
            query.Append(" 	[wo].[Id] + ' - ' + [wo].[Name], ");
            query.Append(" 	[ww].[Id] + ' - ' + [ww].[Name], ");
            query.Append(" 	[rr].[DateWarehouse], ");
            query.Append(" 	[wd].[Id] + ' - ' + [wd].[Name] ");
            //query.Append(" 	[re].[Description]  ");
            query.Append(" ORDER BY [rr].[Id] ");

            var model = db.Database
                .SqlQuery<RefurbRequestExportModel>(query.ToString()).Where(x => x.isDOA == true)
                .ToList();

            return new CsvActionResult<RefurbRequestExportModel>(model,
                string.Format("doa-requests.{0:yyyyMMddHHmm}.csv", DateTime.Now),
                ';');
        }

        private bool validateFile(HttpPostedFileBase File)
        {
            string[] allowedExtensions = { ".xlsx", ".xls" };

            if (File == null)
            {
                ViewBag.Message = "No file selected.";
                return false;
            }
            string extension = Path.GetExtension(File.FileName);
            if (!allowedExtensions.Contains(extension))
            {
                ViewBag.Message = "File extension not supported.";
                return false;
            }

            return true;
        }
    }
}