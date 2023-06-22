using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Mvc;
using WebApplication.Attributes;
using WebApplication.Extensions;
using WebApplication.Models.Application;

namespace WebApplication.Controllers
{
    [AuthorizePermissions(Resource = "Charts")]
    public class ChartsController : Controller
    {
        private List<DateTime> lastSevenDays = Enumerable.Range(0, 7) // 7 days
                .Select(offset => DateTime.Now.AddDays(offset - 6).Date)
                .ToList();

        private List<DateTime> lastThirtyDays = Enumerable.Range(0, 30) // 30 days
                .Select(offset => DateTime.Now.AddDays(offset - 29).Date)
                .ToList();

        private DataModel _dbContext;
        private DataModel DbContext
        {
            get
            {
                return _dbContext ?? (_dbContext = new DataModel());
            }
        }


        #region unused methods
        public ActionResult ChartJs()
        {
            return View();
        }

        public ActionResult Morris()
        {
            return View();
        }

        public ActionResult Flot()
        {
            return View();
        }

        public ActionResult Inline()
        {
            return View();
        }
        #endregion

        [AuthorizePermissions(Resource = "Charts", Operation = "Read")]
        [HttpGet]
        public ActionResult Index()
        {
            //// Usado como data mínima do daterangepicker para evitar que o usuário selecione uma data que retorne null
            //ViewBag.FirstRequestDate = DbContext.RefurbRequests.Min(r => r.DateRequested).ToString("yyyyMMdd");

            ViewBag.FirstRequestDate = DateTime.Today;
                return View();

        }

        [AuthorizePermissions(Resource = "Charts", Operation = "Read")]
        [HttpGet]
        public ActionResult Test()
        {
            return View();
        }

        [AuthorizePermissions(Resource = "Charts", Operation = "Read")]
        [HttpGet]
        public ActionResult StatusReportByDay(string StartDate, string EndDate)
        {
            var query = new StringBuilder();

            #region result without left join on dates
            //query.Append("DECLARE @start INT; ");
            //query.Append("SELECT @start = ");
            //query.Append("	SUM( ");
            //query.Append("		CASE WHEN [Status] = 0 THEN 1 ELSE 0 END ");
            //query.Append("		- CASE WHEN [Status] = 10 THEN 1 ELSE 0 END ");
            //query.Append("	) ");
            //query.Append("FROM [dbo].[RequestFlows] ");
            //query.Append("WHERE [DateRequested] < DATEADD(day, -7, CAST(GETDATE() AS DATE)); ");
            //query.Append("WITH [tmp] AS ( ");
            //query.Append("	SELECT  ");
            //query.Append("		CAST([DateRequested] AS DATE) AS [DateRequested], ");
            //query.Append("		SUM(CASE WHEN [Status] = 0 THEN 1 ELSE 0 END) AS [Received], ");
            //query.Append("		SUM(CASE WHEN [Status] = 10 THEN 1 ELSE 0 END) AS [FinalInspection], ");
            //query.Append("		SUM( ");
            //query.Append("			CASE [Status] ");
            //query.Append("				WHEN 0 THEN 0 ");
            //query.Append("				WHEN 10 THEN 0 ");
            //query.Append("				ELSE 1 ");
            //query.Append("			END ");
            //query.Append("		) AS [InWork] ");
            //query.Append("	FROM [dbo].[RequestFlows] ");
            //query.Append("	WHERE [DateRequested] >= DATEADD(day, -7, CAST(GETDATE() AS DATE)) ");
            //query.Append("	GROUP BY CAST([DateRequested] AS Date) ");
            //query.Append(") ");
            //query.Append("SELECT  ");
            //query.Append("	[DateRequested], ");
            //query.Append("	[Received], ");
            //query.Append("	[FinalInspection], ");
            //query.Append("	[InWork], ");
            //query.Append("	@start ");
            //query.Append("	+ SUM([Received] - [FinalInspection]) OVER (ORDER BY [DateRequested] ROWS UNBOUNDED PRECEDING) AS [Remaining] ");
            //query.Append("FROM [tmp]; ");
            #endregion


            query.AppendLine(" DECLARE @StartDate DATETIME;   ");
            query.AppendLine(" DECLARE @EndDate DATETIME;   ");
            query.AppendLine(" DECLARE @DateDiff INT;   ");
            query.AppendLine(" SET @StartDate = DATEADD(day, -30, GETDATE());   ");
            query.AppendLine(" SET @EndDate = GETDATE();   ");
            if (!String.IsNullOrEmpty(StartDate))
            {
                query.AppendLine(" SET @StartDate = DATEADD(day, ");
                query.AppendLine(DaysDiffFromToday(StartDate));
                query.AppendLine(", GETDATE());   ");
                query.AppendLine(" SET @EndDate = DATEADD(day, ");
                query.AppendLine(DaysDiffFromToday(EndDate));
                query.AppendLine(", GETDATE());   ");
            }

            query.AppendLine(" WITH [dates] AS ( ");
            //query.AppendLine(" 			SELECT DATEADD(day, -30, GETDATE()) AS [Date] ");
            query.AppendLine(" 			SELECT @StartDate AS [Date] ");
            query.AppendLine(" 			UNION ALL  ");
            query.AppendLine(" 			SELECT [Date] + 1 FROM [dates] WHERE [Date] < @EndDate ");
            query.AppendLine(" 		) ");
            query.AppendLine(" SELECT [Date], [Received], [TrialPerformed],  ");
            query.AppendLine(" 		[SentToRepair], [Repaired], [SentToCosmetic],  ");
            query.AppendLine(" 		[CosmeticPerformed], [SentToScrapEvaluation], [SentToBgaScrapEvaluation], ");
            query.AppendLine(" 		[SentToScrap], [SentToBgaScrap], [SentToDOA], [SentToFinalInspection],  ");
            query.AppendLine(" 		[FinalInspection], [SentToCosmeticHold], [SentBackToTrial], [Delivered] ");
            query.AppendLine(" 	FROM ( ");
            query.AppendLine(" 		SELECT  ");
            query.AppendLine(" 			CAST([dates].[Date] AS DATE) [Date],  ");
            query.AppendLine(" 			[dbo].[RequestFlows].[Id], ");
            query.AppendLine(" 			CASE [dbo].[RequestFlows].[Status] ");
            query.AppendLine(" 				WHEN 0 THEN 'Received' ");
            query.AppendLine(" 				WHEN 1 THEN 'TrialPerformed' ");
            query.AppendLine(" 				WHEN 2 THEN 'SentToRepair' ");
            query.AppendLine(" 				WHEN 3 THEN 'Repaired' ");
            query.AppendLine(" 				WHEN 4 THEN 'SentToCosmetic' ");
            query.AppendLine(" 				WHEN 5 THEN 'CosmeticPerformed' ");
            query.AppendLine(" 				WHEN 6 THEN 'SentToScrapEvaluation' ");
            query.AppendLine(" 				WHEN 7 THEN 'SentToScrap' ");
            query.AppendLine(" 				WHEN 8 THEN 'SentToDOA' ");
            query.AppendLine(" 				WHEN 9 THEN 'SentToFinalInspection' ");
            query.AppendLine(" 				WHEN 10 THEN 'FinalInspection' ");
            query.AppendLine(" 				WHEN 11 THEN 'SentToCosmeticHold' ");
            query.AppendLine(" 				WHEN 12 THEN 'SentBackToTrial' ");
            query.AppendLine(" 				WHEN 13 THEN 'Delivered' ");
            query.AppendLine(" 				WHEN 14 THEN 'SentToBgaScrapEvaluation' ");
            query.AppendLine(" 				WHEN 15 THEN 'SentToBgaScrap' ");
            query.AppendLine(" 				ELSE 'N/A' ");
            query.AppendLine(" 			END AS [Status] ");
            query.AppendLine(" 		FROM [dates] ");
            query.AppendLine(" 		LEFT JOIN [dbo].[RequestFlows]  ");
            query.AppendLine(" 			ON CAST([dates].[date] AS DATE) = CAST([dbo].[RequestFlows].[DateRequested] AS DATE) ");
            query.AppendLine(" 		WHERE EXISTS ( ");
            query.AppendLine(" 			SELECT 1  ");
            query.AppendLine(" 			FROM [dbo].[RefurbRequests]  ");
            query.AppendLine(" 			WHERE [dbo].[RefurbRequests].[Cancelled] = 0 ");
            query.AppendLine(" 				AND [dbo].[RefurbRequests].[Id] = [dbo].[RequestFlows].[RequestId] ");
            query.AppendLine(" 			) ");
            query.AppendLine(" 	) AS [src] ");
            query.AppendLine(" 	PIVOT( ");
            query.AppendLine(" 		COUNT(Id) FOR [Status] IN ([Received], [TrialPerformed],  ");
            query.AppendLine(" 		[SentToRepair], [Repaired], [SentToCosmetic],  ");
            query.AppendLine(" 		[CosmeticPerformed], [SentToScrapEvaluation], [SentToBgaScrapEvaluation], ");
            query.AppendLine(" 		[SentToScrap], [SentToBgaScrap], [SentToDOA], [SentToFinalInspection],  ");
            query.AppendLine(" 		[FinalInspection], [SentToCosmeticHold], [SentBackToTrial], [Delivered]) ");
            query.AppendLine(" 	) AS [pivotTable] ");
            query.AppendLine(" 	ORDER BY [Date] ");


            var pivot = DbContext.Database.SqlQuery<StatusByDayReport>(query.ToString()).ToList();


            return Json(pivot, JsonRequestBehavior.AllowGet);
        }

        [AuthorizePermissions(Resource = "Charts", Operation = "Read")]
        [HttpGet]
        public CsvActionResult<StatusByDayReport> StatusReportByDayAsCsv(string StartDateStatus, string EndDateStatus)
        {
            var query = new StringBuilder();

            query.AppendLine(" DECLARE @StartDate DATETIME;   ");
            query.AppendLine(" DECLARE @EndDate DATETIME;   ");
            query.AppendLine(" DECLARE @DateDiff INT;   ");
            query.AppendLine(" SET @StartDate = DATEADD(day, -30, GETDATE());   ");
            query.AppendLine(" SET @EndDate = GETDATE();   ");
            if (!String.IsNullOrEmpty(StartDateStatus))
            {
                query.AppendLine(" SET @StartDate = DATEADD(day, ");
                query.AppendLine(DaysDiffFromToday(StartDateStatus));
                query.AppendLine(", GETDATE());   ");
                query.AppendLine(" SET @EndDate = DATEADD(day, ");
                query.AppendLine(DaysDiffFromToday(EndDateStatus));
                query.AppendLine(", GETDATE());   ");
            }

            query.AppendLine(" WITH [dates] AS ( ");
            //query.AppendLine(" 			SELECT DATEADD(day, -30, GETDATE()) AS [Date] ");
            query.AppendLine(" 			SELECT @StartDate AS [Date] ");
            query.AppendLine(" 			UNION ALL  ");
            query.AppendLine(" 			SELECT [Date] + 1 FROM [dates] WHERE [Date] < @EndDate ");
            query.AppendLine(" 		) ");
            query.AppendLine(" SELECT [Date], [Received], [TrialPerformed],  ");
            query.AppendLine(" 		[SentToRepair], [Repaired], [SentToCosmetic],  ");
            query.AppendLine(" 		[CosmeticPerformed], [SentToScrapEvaluation], [SentToBgaScrapEvaluation], ");
            query.AppendLine(" 		[SentToScrap], [SentToBgaScrap], [SentToDOA], [SentToFinalInspection],  ");
            query.AppendLine(" 		[FinalInspection], [SentToCosmeticHold], [SentBackToTrial], [Delivered] ");
            query.AppendLine(" 	FROM ( ");
            query.AppendLine(" 		SELECT  ");
            query.AppendLine(" 			CAST([dates].[Date] AS DATE) [Date],  ");
            query.AppendLine(" 			[dbo].[RequestFlows].[Id], ");
            query.AppendLine(" 			CASE [dbo].[RequestFlows].[Status] ");
            query.AppendLine(" 				WHEN 0 THEN 'Received' ");
            query.AppendLine(" 				WHEN 1 THEN 'TrialPerformed' ");
            query.AppendLine(" 				WHEN 2 THEN 'SentToRepair' ");
            query.AppendLine(" 				WHEN 3 THEN 'Repaired' ");
            query.AppendLine(" 				WHEN 4 THEN 'SentToCosmetic' ");
            query.AppendLine(" 				WHEN 5 THEN 'CosmeticPerformed' ");
            query.AppendLine(" 				WHEN 6 THEN 'SentToScrapEvaluation' ");
            query.AppendLine(" 				WHEN 7 THEN 'SentToScrap' ");
            query.AppendLine(" 				WHEN 8 THEN 'SentToDOA' ");
            query.AppendLine(" 				WHEN 9 THEN 'SentToFinalInspection' ");
            query.AppendLine(" 				WHEN 10 THEN 'FinalInspection' ");
            query.AppendLine(" 				WHEN 11 THEN 'SentToCosmeticHold' ");
            query.AppendLine(" 				WHEN 12 THEN 'SentBackToTrial' ");
            query.AppendLine(" 				WHEN 13 THEN 'Delivered' ");
            query.AppendLine(" 				WHEN 14 THEN 'SentToBgaScrapEvaluation' ");
            query.AppendLine(" 				WHEN 15 THEN 'SentToBgaScrap' ");
            query.AppendLine(" 				ELSE 'N/A' ");
            query.AppendLine(" 			END AS [Status] ");
            query.AppendLine(" 		FROM [dates] ");
            query.AppendLine(" 		LEFT JOIN [dbo].[RequestFlows]  ");
            query.AppendLine(" 			ON CAST([dates].[date] AS DATE) = CAST([dbo].[RequestFlows].[DateRequested] AS DATE) ");
            query.AppendLine(" 		WHERE EXISTS ( ");
            query.AppendLine(" 			SELECT 1  ");
            query.AppendLine(" 			FROM [dbo].[RefurbRequests]  ");
            query.AppendLine(" 			WHERE [dbo].[RefurbRequests].[Cancelled] = 0 ");
            query.AppendLine(" 				AND [dbo].[RefurbRequests].[Id] = [dbo].[RequestFlows].[RequestId] ");
            query.AppendLine(" 			) ");
            query.AppendLine(" 	) AS [src] ");
            query.AppendLine(" 	PIVOT( ");
            query.AppendLine(" 		COUNT(Id) FOR [Status] IN ([Received], [TrialPerformed],  ");
            query.AppendLine(" 		[SentToRepair], [Repaired], [SentToCosmetic],  ");
            query.AppendLine(" 		[CosmeticPerformed], [SentToScrapEvaluation], [SentToBgaScrapEvaluation], ");
            query.AppendLine(" 		[SentToScrap], [SentToBgaScrap], [SentToDOA], [SentToFinalInspection],  ");
            query.AppendLine(" 		[FinalInspection], [SentToCosmeticHold], [SentBackToTrial], [Delivered]) ");
            query.AppendLine(" 	) AS [pivotTable] ");
            query.AppendLine(" 	ORDER BY [Date] ");


            var pivot = DbContext.Database.SqlQuery<StatusByDayReport>(query.ToString()).ToList();

            return new CsvActionResult<StatusByDayReport>(pivot,
                string.Format("status-by-day.{0:yyyyMMddHHmm}.csv", DateTime.Now),
                ';');
        }

        [AuthorizePermissions(Resource = "Charts", Operation = "Read")]
        [HttpGet]
        public ActionResult ProdutivityReport(string StartDate, string EndDate)
        {
            var query = new StringBuilder();
            query.AppendLine(" IF OBJECT_ID('tempdb.dbo.#facts', 'U') IS NOT NULL  ");
            query.AppendLine("   DROP TABLE #facts; ");
            query.AppendLine("  ");
            query.AppendLine(" WITH [nodups] AS ( ");
            query.AppendLine(" 		SELECT DISTINCT [RequestId], [DateRequested], [Status]  ");
            query.AppendLine(" 		FROM [dbo].[RequestFlows]  ");
            query.AppendLine(" 		WHERE 1 = 1  ");
            query.AppendLine(" 		AND [Status] IN (0, 7, 13, 15) ");
            query.AppendLine(" 	) ");
            query.AppendLine("  ");
            query.AppendLine(" SELECT [DateRequested] AS [DateRequested], [origin84], [origin83], [destination85], [destination86], [destination89] ");
            query.AppendLine(" INTO #facts ");
            query.AppendLine(" FROM ( ");
            query.AppendLine(" 	SELECT ");
            query.AppendLine(" 		CAST([nodups].[DateRequested] AS DATE) [DateRequested], ");
            query.AppendLine(" 		1 AS [Reference], ");
            query.AppendLine(" 		CASE [nodups].[Status] ");
            query.AppendLine(" 		  WHEN 0 THEN 'origin' + [rr].[OriginId] ");
            query.AppendLine(" 		  WHEN 7 THEN 'destination' + [rr].[DestinationId] ");
            query.AppendLine(" 		  WHEN 13 THEN 'destination' + [rr].[DestinationId] ");
            query.AppendLine(" 		  WHEN 15 THEN 'destination' + [rr].[DestinationId] ");
            query.AppendLine(" 		END [Warehouse] ");
            query.AppendLine("  ");
            query.AppendLine(" 	FROM [nodups] ");
            query.AppendLine(" 	INNER JOIN [dbo].[RefurbRequests] [rr]  ");
            query.AppendLine(" 	  ON [nodups].[RequestId] = rr.[Id] AND [rr].[Cancelled] = 0) AS [source] ");
            query.AppendLine(" PIVOT ( ");
            query.AppendLine("   SUM([Reference]) FOR [Warehouse] IN ([origin84], [origin83], [destination85], [destination86], [destination89]) ");
            query.AppendLine(" ) AS [PivotTable] ");
            query.AppendLine(" ; ");
            query.AppendLine("  ");
            query.AppendLine(" DECLARE @Start INT; ");
            query.AppendLine(" DECLARE @StartDate DATETIME; ");
            query.AppendLine(" DECLARE @EndDate DATETIME; ");
            query.AppendLine("  ");
            query.AppendLine(" SET @StartDate = DATEADD(day, -15, GETDATE()); ");
            query.AppendLine(" SET @EndDate = GETDATE(); ");

            if(!String.IsNullOrEmpty(StartDate))
            {
                // DateTime.ParseExact(date, "yyyyMMdd", null)
                var startDateParsed = DateTime.ParseExact(StartDate, "yyyyMMdd", null);
                query.AppendLine(String.Format(" SET @StartDate = '{0:yyyy-MM-dd}'; ", startDateParsed));
            }
            if (!String.IsNullOrEmpty(EndDate))
            {
                // DateTime.ParseExact(date, "yyyyMMdd", null)
                var endDateParsed = DateTime.ParseExact(EndDate, "yyyyMMdd", null);
                query.AppendLine(String.Format(" SET @EndDate = '{0:yyyy-MM-dd}'; ", endDateParsed));
            }

            query.AppendLine("  ");
            query.AppendLine(" SELECT  ");
            query.AppendLine("     @Start = (SUM(ISNULL([origin84], 0))  ");
            query.AppendLine(" 	 + SUM(ISNULL([origin83], 0))) -  ");
            query.AppendLine(" 	(SUM(ISNULL([destination85], 0)) ");
            query.AppendLine(" 	 + SUM(ISNULL([destination89], 0)) + SUM(ISNULL([destination86], 0))) ");
            query.AppendLine("   FROM #facts ");
            query.AppendLine("   WHERE [DateRequested] < @StartDate; ");
            query.AppendLine("  ");
            query.AppendLine(" WITH [dates] AS ( ");
            query.AppendLine(" 	SELECT @StartDate [Date] ");
            query.AppendLine(" 	UNION ALL  ");
            query.AppendLine(" 	SELECT [Date] + 1 FROM [dates] WHERE [Date] < CAST(@EndDate AS DATE) ");
            query.AppendLine(" ) ");
            query.AppendLine("  ");
            query.AppendLine(" SELECT ");
            query.AppendLine("     CAST([dates].[Date] AS DATE) [Date], ");
            query.AppendLine(" 	ISNULL(#facts.[origin84], 0) AS [ReceivedDisconnection],  ");
            query.AppendLine(" 	ISNULL(#facts.[origin83], 0) AS [ReceivedMaintenance],  ");
            query.AppendLine(" 	ISNULL(#facts.[destination85], 0) + ISNULL(#facts.[destination86], 0) AS [DeliveredScrap],  ");
            query.AppendLine(" 	ISNULL(#facts.[destination89], 0) AS [DeliveredMaintenance],  ");
            query.AppendLine(" 	@Start +  ");
            query.AppendLine(" 	SUM ( ");
            query.AppendLine(" 		(ISNULL(#facts.[origin84], 0) + ");
            query.AppendLine(" 		ISNULL(#facts.[origin83], 0) ) - ");
            query.AppendLine(" 		(ISNULL(#facts.[destination85], 0) + ISNULL(#facts.[destination86], 0) + ");
            query.AppendLine(" 		ISNULL(#facts.[destination89], 0) ) ");
            query.AppendLine(" 	) OVER (ORDER BY [dates].[Date] ROWS UNBOUNDED PRECEDING) [Remaining] ");
            query.AppendLine("   FROM [dates] ");
            query.AppendLine("   LEFT JOIN #facts ON CAST([dates].[Date] AS DATE) = #facts.[DateRequested]; ");

            var result = DbContext.Database
                .SqlQuery<ProdutivityReport>(query.ToString())
                .ToList();

            return Json(result, JsonRequestBehavior.AllowGet);
        }

        [AuthorizePermissions(Resource = "Charts", Operation = "Read")]
        [HttpGet]
        public CsvActionResult<ProdutivityReport> ProdutivityReportAsCsv(string StartDate, string EndDate)
        {
            var query = new StringBuilder();
            query.AppendLine(" IF OBJECT_ID('tempdb.dbo.#facts', 'U') IS NOT NULL  ");
            query.AppendLine("   DROP TABLE #facts; ");
            query.AppendLine("  ");
            query.AppendLine(" WITH [nodups] AS ( ");
            query.AppendLine(" 		SELECT DISTINCT [RequestId], [DateRequested], [Status]  ");
            query.AppendLine(" 		FROM [dbo].[RequestFlows]  ");
            query.AppendLine(" 		WHERE 1 = 1  ");
            query.AppendLine(" 		AND [Status] IN (0, 7, 13, 15) ");
            query.AppendLine(" 	) ");
            query.AppendLine("  ");
            query.AppendLine(" SELECT [DateRequested] AS [DateRequested], [origin84], [origin83], [destination85], [destination86], [destination89] ");
            query.AppendLine(" INTO #facts ");
            query.AppendLine(" FROM ( ");
            query.AppendLine(" 	SELECT ");
            query.AppendLine(" 		CAST([nodups].[DateRequested] AS DATE) [DateRequested], ");
            query.AppendLine(" 		1 AS [Reference], ");
            query.AppendLine(" 		CASE [nodups].[Status] ");
            query.AppendLine(" 		  WHEN 0 THEN 'origin' + [rr].[OriginId] ");
            query.AppendLine(" 		  WHEN 7 THEN 'destination' + [rr].[DestinationId] ");
            query.AppendLine(" 		  WHEN 13 THEN 'destination' + [rr].[DestinationId] ");
            query.AppendLine(" 		  WHEN 15 THEN 'destination' + [rr].[DestinationId] ");
            query.AppendLine(" 		END [Warehouse] ");
            query.AppendLine("  ");
            query.AppendLine(" 	FROM [nodups] ");
            query.AppendLine(" 	INNER JOIN [dbo].[RefurbRequests] [rr]  ");
            query.AppendLine(" 	  ON [nodups].[RequestId] = rr.[Id] AND [rr].[Cancelled] = 0) AS [source] ");
            query.AppendLine(" PIVOT ( ");
            query.AppendLine("   SUM([Reference]) FOR [Warehouse] IN ([origin84], [origin83], [destination85], [destination86], [destination89]) ");
            query.AppendLine(" ) AS [PivotTable] ");
            query.AppendLine(" ; ");
            query.AppendLine("  ");
            query.AppendLine(" DECLARE @Start INT; ");
            query.AppendLine(" DECLARE @StartDate DATETIME; ");
            query.AppendLine(" DECLARE @EndDate DATETIME; ");
            query.AppendLine("  ");
            query.AppendLine(" SET @StartDate = DATEADD(day, -15, GETDATE()); ");
            query.AppendLine(" SET @EndDate = GETDATE(); ");

            if (!String.IsNullOrEmpty(StartDate))
            {
                // DateTime.ParseExact(date, "yyyyMMdd", null)
                var startDateParsed = DateTime.ParseExact(StartDate, "yyyyMMdd", null);
                query.AppendLine(String.Format(" SET @StartDate = '{0:yyyy-MM-dd}'; ", startDateParsed));
            }
            if (!String.IsNullOrEmpty(EndDate))
            {
                // DateTime.ParseExact(date, "yyyyMMdd", null)
                var endDateParsed = DateTime.ParseExact(EndDate, "yyyyMMdd", null);
                query.AppendLine(String.Format(" SET @EndDate = '{0:yyyy-MM-dd}'; ", endDateParsed));
            }

            query.AppendLine("  ");
            query.AppendLine(" SELECT  ");
            query.AppendLine("     @Start = (SUM(ISNULL([origin84], 0))  ");
            query.AppendLine(" 	 + SUM(ISNULL([origin83], 0))) -  ");
            query.AppendLine(" 	(SUM(ISNULL([destination85], 0)) + SUM(ISNULL([destination86], 0)) ");
            query.AppendLine(" 	 + SUM(ISNULL([destination89], 0))) ");
            query.AppendLine("   FROM #facts ");
            query.AppendLine("   WHERE [DateRequested] < @StartDate; ");
            query.AppendLine("  ");
            query.AppendLine(" WITH [dates] AS ( ");
            query.AppendLine(" 	SELECT @StartDate [Date] ");
            query.AppendLine(" 	UNION ALL  ");
            query.AppendLine(" 	SELECT [Date] + 1 FROM [dates] WHERE [Date] < CAST(@EndDate AS DATE) ");
            query.AppendLine(" ) ");
            query.AppendLine("  ");
            query.AppendLine(" SELECT ");
            query.AppendLine("     CAST([dates].[Date] AS DATE) [Date], ");
            query.AppendLine(" 	ISNULL(#facts.[origin84], 0) AS [ReceivedDisconnection],  ");
            query.AppendLine(" 	ISNULL(#facts.[origin83], 0) AS [ReceivedMaintenance],  ");
            query.AppendLine(" 	ISNULL(#facts.[destination85], 0) + ISNULL(#facts.[destination86], 0) AS [DeliveredScrap],  ");
            query.AppendLine(" 	ISNULL(#facts.[destination89], 0) AS [DeliveredMaintenance],  ");
            query.AppendLine(" 	@Start +  ");
            query.AppendLine(" 	SUM ( ");
            query.AppendLine(" 		(ISNULL(#facts.[origin84], 0) + ");
            query.AppendLine(" 		ISNULL(#facts.[origin83], 0) ) - ");
            query.AppendLine(" 		(ISNULL(#facts.[destination85], 0) + ISNULL(#facts.[destination86], 0) + ");
            query.AppendLine(" 		ISNULL(#facts.[destination89], 0) ) ");
            query.AppendLine(" 	) OVER (ORDER BY [dates].[Date] ROWS UNBOUNDED PRECEDING) [Remaining] ");
            query.AppendLine("   FROM [dates] ");
            query.AppendLine("   LEFT JOIN #facts ON CAST([dates].[Date] AS DATE) = #facts.[DateRequested]; ");

            var result = DbContext.Database
                .SqlQuery<ProdutivityReport>(query.ToString())
                .ToList();

            return new CsvActionResult<ProdutivityReport>(result,
                string.Format("produtivity-report.{0:yyyyMMddHHmm}.csv", DateTime.Now),
                ';');
        }

        [AuthorizePermissions(Resource = "Charts", Operation = "Read")]
        [HttpGet]
        public ActionResult StatusReport()
        {
            // card https://trello.com/c/eqKiHBJr
            // remover os registros cancelados das contagens
            var notAllowedStatus = new[] {
                RequestFlowStatus.Delivered,
                RequestFlowStatus.SentToScrap,
                RequestFlowStatus.SentToBgaScrap
            };

            var requests = DbContext.RefurbRequests
                .Where(rr => !rr.Cancelled && !notAllowedStatus.Contains(rr.Status))
            // eo card https://trello.com/c/eqKiHBJr
                .GroupBy(rr => rr.Status)
                .Select(gr => new
                {
                    status = gr.Key,
                    count = gr.Count()
                })
                .ToList()
                .Select(o => new
                {
                    status = Utility.GetDescriptionFromEnumValue((RequestFlowStatus)o.status),
                    count = o.count
                });

            return Json(requests, JsonRequestBehavior.AllowGet);
        }

        [AuthorizePermissions(Resource = "Charts", Operation = "Read")]
        [HttpGet]
        public CsvActionResult<StatusReport> StatusReportAsCsv()
        {
            // card https://trello.com/c/eqKiHBJr
            // remover os registros cancelados das contagens
            var notAllowedStatus = new[] {
                RequestFlowStatus.Delivered,
                RequestFlowStatus.SentToScrap,
                RequestFlowStatus.SentToBgaScrap
            };

            var requests = DbContext.RefurbRequests
                .Where(rr => !rr.Cancelled && !notAllowedStatus.Contains(rr.Status))
            // eo card https://trello.com/c/eqKiHBJr
                .GroupBy(rr => rr.Status)
                .Select(gr => new StatusReport
                {
                    Status = gr.Key,
                    Count = gr.Count()
                })
                .ToList();

            return new CsvActionResult<StatusReport>(requests,
                string.Format("status-report.{0:yyyyMMddHHmm}.csv", DateTime.Now),
                ';');
        }


        [AuthorizePermissions(Resource = "Charts", Operation = "Read")]
        [HttpGet]
        public ActionResult AgingByStatus()
        {
            var query = new StringBuilder();
            query.AppendLine(" WITH [Aging] AS (");
            query.AppendLine(" 	SELECT ");
            query.AppendLine("		[RequestId],");
            query.AppendLine("		CAST([DateRequested] as Date) [DateRequested],");
            query.AppendLine("		[Status],");
            query.AppendLine("		DATEDIFF(day, [DateRequested], LEAD([DateRequested], 1, GETDATE()) OVER(PARTITION BY [RequestId] ORDER BY [DateRequested])) AS [Aging]");
            query.AppendLine("	FROM [dbo].[RequestFlows]");
            query.AppendLine(" 		WHERE EXISTS ( ");
            query.AppendLine(" 			SELECT 1  ");
            query.AppendLine(" 			FROM [dbo].[RefurbRequests]  ");
            query.AppendLine(" 			WHERE [dbo].[RefurbRequests].[Cancelled] = 0 ");
            query.AppendLine(" 				AND [dbo].[RefurbRequests].[Id] = [dbo].[RequestFlows].[RequestId] ");
            query.AppendLine(" 			) ");
            query.AppendLine(" ) ");
            query.AppendLine(" SELECT [Status], AVG([Aging]) AS [AgingAvg] FROM [Aging] GROUP BY [Status]");

            var aging = DbContext.Database
                .SqlQuery<AgingReport>(query.ToString())
                .Select(r => new
                {
                    status = Utility.GetDescriptionFromEnumValue((RequestFlowStatus)r.Status),
                    aging = r.AgingAvg
                })
                .ToList();

            return Json(aging, JsonRequestBehavior.AllowGet);
        }


        [AuthorizePermissions(Resource = "Charts", Operation = "Read")]
        [HttpGet]
        public CsvActionResult<AgingReport> AgingByStatusAsCsv()
        {
            var query = new StringBuilder();
            query.AppendLine(" WITH [Aging] AS (");
            query.AppendLine(" 	SELECT ");
            query.AppendLine("		[RequestId],");
            query.AppendLine("		CAST([DateRequested] as Date) [DateRequested],");
            query.AppendLine("		[Status],");
            query.AppendLine("		DATEDIFF(day, [DateRequested], LEAD([DateRequested], 1, GETDATE()) OVER(PARTITION BY [RequestId] ORDER BY [DateRequested])) AS [Aging]");
            query.AppendLine("	FROM [dbo].[RequestFlows]");
            query.AppendLine(" 		WHERE EXISTS ( ");
            query.AppendLine(" 			SELECT 1  ");
            query.AppendLine(" 			FROM [dbo].[RefurbRequests]  ");
            query.AppendLine(" 			WHERE [dbo].[RefurbRequests].[Cancelled] = 0 ");
            query.AppendLine(" 				AND [dbo].[RefurbRequests].[Id] = [dbo].[RequestFlows].[RequestId] ");
            query.AppendLine(" 			) ");
            query.AppendLine(" ) ");
            query.AppendLine(" SELECT [Status], AVG([Aging]) AS [AgingAvg] FROM [Aging] GROUP BY [Status]");

            var aging = DbContext.Database
                .SqlQuery<AgingReport>(query.ToString())
                .ToList();

            return new CsvActionResult<AgingReport>(aging,
                string.Format("aging-by-status.{0:yyyyMMddHHmm}.csv", DateTime.Now),
                ';');
        }

        [AuthorizePermissions(Resource = "Charts", Operation = "Read")]
        [HttpGet]
        public JsonResult GroupedAgingReport()
        {
            var query = new StringBuilder();
            query.Append(" SELECT [Sum0to10], [Sum11to20], [Sum21to30], [SumGT30], [SumGT60] ");
            query.Append(" FROM ( ");
            query.Append(" 	SELECT	CASE  ");
            query.Append(" 				WHEN DATEDIFF(day, [rr].DateRequested, GETDATE()) <= 10 THEN 'Sum0to10' ");
            query.Append(" 				WHEN DATEDIFF(day, [rr].DateRequested, GETDATE()) BETWEEN 11 AND 20 THEN 'Sum11to20' ");
            query.Append(" 				WHEN DATEDIFF(day, [rr].DateRequested, GETDATE()) BETWEEN 21 AND 30 THEN 'Sum21to30' ");
            query.Append(" 				WHEN DATEDIFF(day, [rr].DateRequested, GETDATE()) BETWEEN 31 AND 59 THEN 'SumGT30' ");
            query.Append(" 				ELSE 'SumGT60' ");
            query.Append(" 			END AS [AgingGroup], ");
            query.Append(" 			COUNT(1) AS [Refurbs] ");
            query.Append(" 	FROM	[dbo].[RefurbRequests] AS [rr] ");
            query.Append(" 	WHERE	[rr].[Status] NOT IN (7, 13, 15) ");
            query.Append(" 	  AND   [rr].[Cancelled] = 0 ");
            query.Append(" 	GROUP	BY CASE  ");
            query.Append(" 				WHEN DATEDIFF(day, [rr].DateRequested, GETDATE()) <= 10 THEN 'Sum0to10' ");
            query.Append(" 				WHEN DATEDIFF(day, [rr].DateRequested, GETDATE()) BETWEEN 11 AND 20 THEN 'Sum11to20' ");
            query.Append(" 				WHEN DATEDIFF(day, [rr].DateRequested, GETDATE()) BETWEEN 21 AND 30 THEN 'Sum21to30' ");
            query.Append(" 				WHEN DATEDIFF(day, [rr].DateRequested, GETDATE()) BETWEEN 31 AND 59 THEN 'SumGT30' ");
            query.Append(" 				ELSE 'SumGT60' ");
            query.Append(" 			END) AS [src] ");
            query.Append(" PIVOT ( ");
            query.Append(" MAX([src].[Refurbs]) ");
            query.Append(" FOR [AgingGroup] IN ([Sum0to10], [Sum11to20], [Sum21to30], [SumGT30], [SumGT60]) ");
            query.Append(" ) AS [table] ");

            var aging = DbContext.Database
                .SqlQuery<GroupedAging>(query.ToString())
                .ToList();

            return Json(aging, JsonRequestBehavior.AllowGet);
        }

        [AuthorizePermissions(Resource = "Charts", Operation = "Read")]
        [HttpGet]
        public CsvActionResult<GroupedAgingCsv> GroupedAgingReportAsCsv()
        {
            var query = new StringBuilder();
            query.Append(" SELECT	[rr].[Id], ");
            query.Append(" 		[rr].[DateRequested], ");
            query.Append(" 		[rr].[SerialNumber], ");
            query.Append(" 		CASE [rr].[Status]   ");
            query.Append(" 			WHEN 0 THEN 'Received'   ");
            query.Append(" 			WHEN 1 THEN 'TrialPerformed'   ");
            query.Append(" 			WHEN 2 THEN 'SentToRepair'   ");
            query.Append(" 			WHEN 3 THEN 'Repaired'   ");
            query.Append(" 			WHEN 4 THEN 'SentToCosmetic'   ");
            query.Append(" 			WHEN 5 THEN 'CosmeticPerformed'   ");
            query.Append(" 			WHEN 6 THEN 'SentToScrapEvaluation'   ");
            query.Append(" 			WHEN 7 THEN 'SentToScrap'   ");
            query.Append(" 			WHEN 8 THEN 'SentToDOA'   ");
            query.Append(" 			WHEN 9 THEN 'SentToFinalInspection'   ");
            query.Append(" 			WHEN 10 THEN 'FinalInspection'   ");
            query.Append(" 			WHEN 11 THEN 'SentToCosmeticHold'   ");
            query.Append(" 			WHEN 12 THEN 'SentBackToTrial'   ");
            query.Append(" 			WHEN 13 THEN 'Delivered'   ");
            query.Append(" 			WHEN 14 THEN 'SentToBgaScrapEvaluation'   ");
            query.Append(" 			WHEN 15 THEN 'SentToBgaScrap'   ");
            query.Append(" 			ELSE 'N/A'   ");
            query.Append(" 		END AS [Status], ");
            query.Append(" 		[r].[Date] AS [RepairedAt], ");
            query.Append(" 		[r].[Description] AS [RepairDescription], ");
            query.Append(" 		DATEDIFF(day, [rr].DateRequested, GETDATE()) AS [Aging], ");
            query.Append(" 		CASE  ");
            query.Append(" 			WHEN DATEDIFF(day, [rr].DateRequested, GETDATE()) <= 10 THEN 'Soma de 0-10' ");
            query.Append(" 			WHEN DATEDIFF(day, [rr].DateRequested, GETDATE()) BETWEEN 11 AND 20 THEN 'Soma de 11-20' ");
            query.Append(" 			WHEN DATEDIFF(day, [rr].DateRequested, GETDATE()) BETWEEN 21 AND 30 THEN 'Soma de 21-30' ");
            query.Append(" 			WHEN DATEDIFF(day, [rr].DateRequested, GETDATE()) BETWEEN 31 AND 59 THEN 'Soma de >30' ");
            query.Append(" 			ELSE 'Soma de >60' ");
            query.Append(" 		END AS [AgingGroup] ");
            query.Append(" FROM	[dbo].[RefurbRequests] AS [rr] ");
            query.Append(" LEFT	JOIN [dbo].[Repairs] as [r] ON [rr].[Id] = [r].[RequestId] ");
            query.Append(" WHERE	[rr].[Status] NOT IN (7, 13, 15) ");
            query.Append(" 	  AND   [rr].[Cancelled] = 0; ");

            var aging = DbContext.Database
                .SqlQuery<GroupedAgingCsv>(query.ToString())
                .ToList();

            return new CsvActionResult<GroupedAgingCsv>(aging,
                string.Format("grouped-aging.{0:yyyyMMddHHmm}.csv", DateTime.Now),
                ';');
        }

        /// <summary>
        /// Retorna o número de dias, em string, da data informada em relação ao dia de hoje.
        /// </summary>
        /// <param name="date">No formato "yyyyMMdd"</param>
        /// <returns></returns>
        public string DaysDiffFromToday(string date)
        {
            return (DateTime.ParseExact(date, "yyyyMMdd", null) - DateTime.Now).Days.ToString();
        }
    }

    public class StatusByDayReport
    {
        public DateTime Date { get; set; }
        public int Received { get; set; }
        public int TrialPerformed { get; set; }
        public int SentToRepair { get; set; }
        public int Repaired { get; set; }
        public int SentToCosmetic { get; set; }
        public int CosmeticPerformed { get; set; }
        public int SentToScrapEvaluation { get; set; }
        public int SentToScrap { get; set; }
        public int SentToDOA { get; set; }
        public int SentToFinalInspection { get; set; }
        public int FinalInspection { get; set; }
        public int SentToCosmeticHold { get; set; }
        public int SentBackToTrial { get; set; }
        public int Delivered { get; set; }
        public int SentToBgaScrapEvaluation { get; set; }
        public int SentToBgaScrap { get; set; }
    }

    public class ProdutivityReport
    {
        public DateTime Date { get; set; }
        public int ReceivedDisconnection { get; set; }
        public int ReceivedMaintenance { get; set; }
        public int DeliveredScrap { get; set; }
        public int DeliveredMaintenance { get; set; }
        public int Remaining { get; set; }

    }

    public class AgingReport
    {
        public RequestFlowStatus Status { get; set; }
        public int AgingAvg { get; set; }
    }

    public class StatusReport
    {
        public RequestFlowStatus Status { get; set; }
        public int Count { get; set; }
    }

    public class GroupedAging
    {
        public int? Sum0to10 { get; set; }
        public int? Sum11to20 { get; set; }
        public int? Sum21to30 { get; set; }
        public int? SumGT30 { get; set; }
        public int? SumGT60 { get; set; }
    }

    public class GroupedAgingCsv
    {
        public int Id { get; set; }
        public DateTime DateRequested { get; set; }
        public string SerialNumber { get; set; }
        public string Status { get; set; }
        public DateTime? RepairedAt { get; set; }
        public string RepairDescription { get; set; }
        public int Aging { get; set; }
        public string AgingGroup { get; set; }
    }
}