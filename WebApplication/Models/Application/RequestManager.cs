using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.Entity;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq.Dynamic;

namespace WebApplication.Models.Application
{


    public class RequestManager
    {
        DataModel DB = new DataModel();

        public RequestManager() { }

        /*Flow Methods*/
        public RequestFlow CreateFlow(RefurbRequest Request, string UserId, RequestFlowStatus Status)
        {
            var r = new RequestFlow(Request.Id, UserId, Status);
            DB.RequestFlows.Add(r);
            DB.SaveChanges();
            return r;
        }

        public RequestFlow CreateFlow(RefurbRequest Request)
        {
            var r = new RequestFlow(Request.Id, Request.UserId, Request.Status);
            DB.RequestFlows.Add(r);
            DB.SaveChanges();
            return r;
        }

        //public RequestFlow CreateFlow(RefurbRequest Request, string UserId)
        //{
        //    var r = new RequestFlow(Request.Id, UserId, Request.Status);
        //    DB.RequestFlows.Add(r);
        //    DB.SaveChanges();
        //    return r;
        //}

        // For custom requests
        public RequestFlow CreateFlow(int RequestId, string UserId, string Description, RequestFlowStatus Status)
        {
            var r = new RequestFlow(RequestId, UserId, Status, Description);
            DB.RequestFlows.Add(r);
            DB.SaveChanges();
            return r;
        }

        //onHold
        public RequestFlow CreateFlow(RefurbRequest Request, string UserId, string CosmeticStatus)
        {
            var r = new RequestFlow(Request.Id, UserId, RequestFlowStatus.SentToCosmeticHold, CosmeticStatus);
            DB.RequestFlows.Add(r);
            DB.SaveChanges();
            return r;
        }

        /********************** REQUEST *************************/
        public RefurbRequest GetRequest(int id)
        {
            var r = DB.RefurbRequests.Find(id);
            r.MissingChecklist = (from m in DB.MissingChecklists
                                  where m.RequestId == r.Id
                                  select m).ToList();

            r.CosmeticChecklist = (from m in DB.CosmeticChecklists
                                   where m.RequestId == r.Id
                                   select m).ToList();

            r.RequestFlows = (from m in DB.RequestFlows
                              where m.RequestId == r.Id
                              select m).ToList();
                      

            return r;
        }

        public Warranty GetRequestWarranty(int id)
        {
            return DB.Warranties.FirstOrDefault(w => w.RequestId == id);
        }

        public Trial GetRequestTrial(int id)
        {
            return DB.Trials.FirstOrDefault(w => w.RequestId == id);          
        }
        public CosmeticOverview GetRequestCosmeticOverView(int id)
        {
            return DB.CosmeticOverviews.FirstOrDefault(w => w.Id == id);
        }

        public string GetStatusCosmetic(int id)
        {
            var r = DB.RefurbRequests.Find(id);
            return r.StatusCosmetic;
        }

        public List<RefurbRequest> SearchRequests(Dictionary<string, object> param)
        {
            List<string> where = new List<string>();
            foreach (KeyValuePair<string, object> item in param)
            {
                where.Add(item.Key + ".Contains(@" + item.Key + ")");
            }
            return DB.RefurbRequests.AsQueryable().Where(String.Join(" || ", where), param.ToArray()).ToList<RefurbRequest>();

        }


        //public IQueryable<BatchProducts> GetBatchProductsModelandBatchStockID(int modelId, int batchId)
        //{
        //    var ret = from r in DB.BatchProducts
        //              where r.ModelId == modelId && r.BatchStockId == batchId
        //              select r.Id;

        //    return ret.First();

        //}

        /*Recebe a unidade para reaparo*/
        public RefurbRequest ReceiveRequest(RefurbRequest obj, string HardwareOverviewList, string CosmeticOverviewList)
        {
            /* Bloqueio para não permitir entrar um equipamento com serial number ainda em 
             *      processamento pelo sistema */

            ValidateSNStillProcessing(obj.SerialNumber);

            /* e o bloqueio por serial number */

            // Verifica se o request encaixa em DOA
            obj.isDOA = VerifyIfDOASingle(obj.SerialNumber, obj.DateRequested);

            WarrantyCheck(obj);

            if (obj.Id == 0)
                DB.RefurbRequests.Add(obj);
            else
                DB.Entry(obj).State = EntityState.Modified;
            DB.SaveChanges();
            if (HardwareOverviewList != null)
                this.SetMissingChecklist(obj.Id, HardwareOverviewList.Split(','));
            if (CosmeticOverviewList != null)
                this.SetCosmeticChecklist(obj.Id, CosmeticOverviewList.Split(','));
            this.CreateFlow(obj);
            return obj;
        }

        public bool VerifyIfDOASingle(string serial, DateTime data)
        {
            if (DB.DOAs.Any(r => serial == r.SerialNumber && DbFunctions.DiffDays(data, r.DateReceived) < 730)) return true;
            return false;
        }

        public IQueryable<HardwareOverview> HardwareOverviews()
        {
            return from r in DB.HardwareOverviews
                   select r;
        }
        

        public void SetMissingChecklist(int RequestId, string[] HardwareOverviewList)
        {
            foreach (string i in HardwareOverviewList)
            {
                DB.MissingChecklists.Add(new MissingChecklist(RequestId, int.Parse(i)));
            }
            DB.SaveChanges();
        }

        public IQueryable<CosmeticOverview> CosmeticOverviews()
        {
            return from r in DB.CosmeticOverviews
                   select r;
        }

        public void SetCosmeticChecklist(int RequestId, string[] CosmeticOverviewList)
        {
            foreach (string i in CosmeticOverviewList)
            {
                DB.CosmeticChecklists.Add(new CosmeticChecklist(RequestId, int.Parse(i)));
            }
            DB.SaveChanges();
        }

        public void CosmeticChecklists (int requestId,string CosmeticChecklists)
        {
            if (CosmeticChecklists != null) {
            this.SetCosmeticChecklist(requestId, CosmeticChecklists.Split(','));
        }


    }
        public Trial PerformTrial(Trial obj, RequestFlowStatus Action, string FunctionalTestList)
        {
            if (obj.Id == 0)
                DB.Trials.Add(obj);
            var rf = this.GetRequest(obj.RequestId);
            //rf.Status = Action;
            //DB.Entry(rf);
            //DB.SaveChanges();

            UpdateRequest(obj.RequestId, Action);

            if (FunctionalTestList != null) // Send to repair
                this.SetTrialFunctionalTest(obj.Id, FunctionalTestList.Split(','));
            this.CreateFlow(obj.RequestId, obj.UserId, obj.Description, RequestFlowStatus.TrialPerformed);
            if (Action != RequestFlowStatus.TrialPerformed)
            {
                this.CreateFlow(obj.RequestId, obj.UserId, obj.Description, Action);
            }
            return obj;
        }

        //public Trial PerformTrial(Trial obj, RequestFlowStatus Action, string FunctionalTestList)
        //{
        //    if (obj.Id == 0)
        //        DB.Trials.Add(obj);
        //    this.UpdateRequest(obj.RequestId, Action);
        //    if (FunctionalTestList != null)
        //        this.SetTrialFunctionalTest(obj.Id, FunctionalTestList.Split(','));
        //    this.CreateFlow(obj.RequestId, obj.UserId, obj.Description, RequestFlowStatus.TrialPerformed);
        //    return obj;
        //}

        public void BackToTrial(RefurbRequest Request, string UserId, string Description)
        {
            //var status = RequestFlowStatus.Received;
            //Request.Status = status;
            //DB.Entry(Request);
            //DB.SaveChanges();

            UpdateRequest(Request.Id, RequestFlowStatus.Received);

            this.CreateFlow(Request.Id, UserId, Description, RequestFlowStatus.SentBackToTrial);
        }

        public IQueryable<RepairType> RepairTypes()
        {
            return from r in DB.RepairTypes
                   select r;
        }

        public IQueryable<FunctionalTest> FunctionalTests()
        {
            return from r in DB.FunctionalTests
                   select r;
        }

        public IQueryable<BatchProducts> ModelBatchOrder(int id)
        {
            return from r in DB.BatchProducts
                   where r.BatchStock.Id == id
                   select r;
        }

        public IQueryable<Model> ModelId(int id)
        {
            return from r in DB.Models
                   where r.Id == id
                   select r;
        }

        public IQueryable<CosmeticOverview> CosmeticOverview()
        {
            return from r in DB.CosmeticOverviews
                   select r;
        }

        public void SetRepairRepairTypes(int RepairId, string[] RepairTypeList)
        {
            foreach (string i in RepairTypeList)
            {
                DB.TrialRepairTypes.Add(new RepairRepairType(RepairId, int.Parse(i)));
            }
            DB.SaveChanges();
        }

        public void SetTrialFunctionalTest(int TrialId, string[] FunctionalTestList)
        {
            foreach (string i in FunctionalTestList)
            {
                DB.TrialFunctionalTests.Add(new TrialFunctionalTest(TrialId, int.Parse(i)));
            }
            DB.SaveChanges();
        }



        public Cosmetic PerformCosmetic(Cosmetic obj)
        {
            if (obj.Id == 0)
                DB.Cosmetics.Add(obj);
            this.UpdateRequest(obj.RequestId, RequestFlowStatus.SentToFinalInspection);
            this.CreateFlow(obj.RequestId, obj.UserId, obj.Description, RequestFlowStatus.CosmeticPerformed);
            return obj;
        }

        public FinalInspection UpdateStatus(FinalInspection obj, RequestFlowStatus status)
        {
           
            this.UpdateRequest(obj.RequestId, status);
            this.CreateFlow(obj.RequestId, obj.UserId, obj.Description, RequestFlowStatus.FinalInspection);
            return obj;

        }

        public Repair PerformRepair(Repair obj, RequestFlowStatus Status, string RepairTypeList)
        {
            if (obj.Id == 0)
                DB.Repairs.Add(obj);
            this.UpdateRequest(obj.RequestId, Status);
            if (RepairTypeList != null)
                this.SetRepairRepairTypes(obj.Id, RepairTypeList.Split(','));
            this.CreateFlow(obj.RequestId, obj.UserId, obj.Description, RequestFlowStatus.Repaired);
            if (Status != RequestFlowStatus.Repaired)
            {
                this.CreateFlow(obj.RequestId, obj.UserId, obj.Description, Status);
            }
            return obj;
        }

        public Scrap PerformScrap(Scrap obj)
        {
            if (obj.Id == 0)
                DB.Scraps.Add(obj);
            this.UpdateRequest(obj.RequestId, RequestFlowStatus.SentToScrap);
            this.CreateFlow(obj.RequestId, obj.UserId, obj.Description, RequestFlowStatus.SentToScrap);
            return obj;
        }

        public FinalInspection PerformFinalInspection(FinalInspection obj)
        {
            if (obj.Id == 0)
                DB.FinalInspections.Add(obj);
            this.UpdateRequest(obj.RequestId, RequestFlowStatus.FinalInspection);
            this.CreateFlow(obj.RequestId, obj.UserId, obj.Description, RequestFlowStatus.FinalInspection);
            return obj;
        }

        public Delivery PerformDelivery(Delivery obj)
        {
            if (obj.Id == 0)
                DB.Deliveries.Add(obj);
            this.UpdateRequest(obj.RequestId, RequestFlowStatus.Delivered);
            this.CreateFlow(obj.RequestId, obj.UserId, obj.Description, RequestFlowStatus.Delivered);
            return obj;
        }

        public void UpdateRequest(int requestId, RequestFlowStatus toStatus)
        {
            var rf = this.GetRequest(requestId);
            rf.Status = toStatus;
            rf.LastUpdated = DateTime.Now;

            var warehouse = DB.WarehouseRequestStatuses
                .FirstOrDefault(w => w.RequestFlowStatus == toStatus);

            if (warehouse != null)
            {
                var warehouseChangeTrigger = new[] {
                    RequestFlowStatus.SentToScrapEvaluation,
                    RequestFlowStatus.SentToBgaScrapEvaluation,
                    RequestFlowStatus.SentToScrap,
                    RequestFlowStatus.SentToBgaScrap,
                    RequestFlowStatus.Delivered,
                    RequestFlowStatus.FinalInspection
                };

                if (warehouseChangeTrigger.Contains(toStatus))
                {
                    rf.DestinationId = warehouse.WarehouseId;
                    rf.DateDestination = DateTime.Now;
                }
                else
                {
                    rf.WarehouseId = warehouse.WarehouseId;
                    rf.DateWarehouse = DateTime.Now;
                }
            }

            DB.Entry(rf);
            DB.SaveChanges();
        }


        /*Listas Por Status*/
        public List<RefurbRequest> RequestsByStatus(RequestFlowStatus Status = RequestFlowStatus.Received)
        {
            if (Status == RequestFlowStatus.FinalInspection)
            {
                var ret = from r in DB.RefurbRequests
                          where r.Status == RequestFlowStatus.FinalInspection
                          || r.Status == RequestFlowStatus.SentToScrap
                          && !r.Cancelled
                          select r;
                return ret.ToList();
            }
            else
            {
                var ret = from r in DB.RefurbRequests
                          where r.Status == Status && !r.Cancelled
                          select r;
                return ret.ToList();
            }
        }

        public List<RefurbRequest> RequestsByStatus(RequestFlowStatus status, List<string> where, object[] param)
        {
            if (status == RequestFlowStatus.FinalInspection)
            {
                var ret = from r in DB.RefurbRequests
                          where r.Status == RequestFlowStatus.FinalInspection
                          || r.Status == RequestFlowStatus.SentToScrap
                          && !r.Cancelled
                          select r;
                ret = ret.AsQueryable().Where(String.Join(" || ", where), param);
                return ret.ToList();
            }
            else
            {
                var ret = from r in DB.RefurbRequests
                          where r.Status == status && !r.Cancelled
                          select r;
                ret = ret.AsQueryable().Where(String.Join(" || ", where), param);
                return ret.ToList();
            }
        }

        public List<RefurbRequest> RequestsByCosmeticStatus(string Status)
        {
            IQueryable<RefurbRequest> ret;
            if (Status != null)
            {
                ret = from r in DB.RefurbRequests
                      where r.StatusCosmetic == Status
                      select r;
            }
            else
            {
                ret = from r in DB.RefurbRequests
                      where r.StatusCosmetic != null
                      select r;
            }

            return ret.ToList();
        }


        public IQueryable<RefurbRequest> ReceivedRequests()
        {
            return from r in DB.RefurbRequests
                   where r.Status == RequestFlowStatus.Received && !r.Cancelled
                   select r;
        }

        public IQueryable ScreeningPerformedRequests()
        {
            return from r in DB.RefurbRequests
                   where r.Status == RequestFlowStatus.TrialPerformed && !r.Cancelled
                   select r;
        }

        public IQueryable<RefurbRequest> SentToRefurbRequests()
        {
            return from r in DB.RefurbRequests
                   where r.Status == RequestFlowStatus.SentToRepair && !r.Cancelled
                   select r;
        }

        public IQueryable<RefurbRequest> SentToCosmeticRequests()
        {
            return from r in DB.RefurbRequests
                   where r.Status == RequestFlowStatus.SentToCosmetic && !r.Cancelled
                   select r;
        }

        public IQueryable SentToScrapRequests()
        {
            return from r in DB.RefurbRequests
                   where r.Status == RequestFlowStatus.SentToScrap && !r.Cancelled
                   select r;
        }

        public IQueryable<RefurbRequest> SentToExpedite()
        {
            return from r in DB.RefurbRequests
                   where r.Status == RequestFlowStatus.SentToFinalInspection && !r.Cancelled
                   select r;
        }

        /// <summary>
        /// Rule validation to check if is another request in processing to @serialNumber.
        /// This function results in an Invalid Operation Exception to cancel the action.
        /// </summary>
        /// <param name="serialNumber">Equipment serial number</param>
        public void ValidateSNStillProcessing(string serialNumber)
        {
            var statuses = new[] { RequestFlowStatus.SentToScrap, RequestFlowStatus.Delivered };
            if (DB.RefurbRequests.Any(rr => rr.SerialNumber == serialNumber
            && !rr.Cancelled && !statuses.Contains(rr.Status)))
            {
                throw new InvalidOperationException(
                    String.Format("You cannot use the serial number {0} to create a request while " +
                        "another refurb is still processing.",
                    serialNumber));
            }
        }

        /// <summary>
        /// Rule validation to check and block a serial number @serialNumber delivered at least @days days
        /// </summary>
        /// <param name="serialNumber">Equipment serial number</param>
        /// <param name="days">Days to block the request</param>
        public void ValidateSNInWarranty(string serialNumber, int days)
        {
            var minDate = DateTime.Now.Subtract(TimeSpan.FromDays(days)).Date;
            var statuses = new[] { RequestFlowStatus.SentToScrap, RequestFlowStatus.Delivered };
            if (DB.RefurbRequests.Any(rr => rr.SerialNumber == serialNumber
                    && !rr.Cancelled 
                    && statuses.Contains(rr.Status)
                    && rr.LastUpdated >= minDate))
            {
                throw new InvalidOperationException(
                    String.Format("You cannot use the serial number {0} to create a request while " +
                        "another refurb is delivered in last {1} days",
                    serialNumber, days));
            }
        }

        public string GetSupplyDescription(int RequestId)
        {
            if (DB.Cosmetics.Any(x => x.RequestId == RequestId))
            {
                var supplyId = DB.Cosmetics.FirstOrDefault(x => x.RequestId == RequestId).SupplyId;
                return DB.Supplies.FirstOrDefault(x => x.Id == supplyId).Description;
            }

            return "-";
        }

        public void WarrantyCheck(RefurbRequest request)
        {
            //var parameters = DB.Parameters.FirstOrDefault(x => x.Name == "batch.import.daysToBlockImportByWarranty");
            //var days = Int32.Parse(parameters.Value);
            //var minDate = DateTime.Now.Subtract(TimeSpan.FromDays(days * -1)).Date;

            //if (DB.RefurbRequests.Any(rr => rr.Status == RequestFlowStatus.Delivered && rr.SerialNumber == request.SerialNumber && DbFunctions.DiffDays(request.DateRequested, rr.LastUpdated) <= int.Parse(dias)))
            //{
            //    DB.Warranties.Add(new Warranty
            //    {
            //        RequestId = request.Id,
            //        InWarranty = true,
            //    });
            //}

            var parameters = DB.Parameters.FirstOrDefault(x => x.Name == "batch.import.daysToBlockImportByWarranty");
            var days = Int32.Parse(parameters.Value);
            var minDate = DateTime.Now.Subtract(TimeSpan.FromDays(days)).Date;
            var statuses = new[] { RequestFlowStatus.SentToScrap, RequestFlowStatus.Delivered };

            if (!request.Cancelled
                    && statuses.Contains(request.Status)
                    && request.LastUpdated.Date >= minDate)
            {
                DB.Warranties.Add(new Warranty
                {
                    RequestId = request.Id,
                    InWarranty = true,
                });
                DB.SaveChanges();
            }
        }

        public void UpdateWarranty(int RequestId, bool InWarranty, string WarrantyDescription)
        {
            var warranty = DB.Warranties.FirstOrDefault(w => w.RequestId == RequestId);

            if (warranty != null)
            {
                warranty.InWarranty = InWarranty;
                warranty.Description = InWarranty ? null : WarrantyDescription;

                DB.Entry(warranty).State = EntityState.Modified;
                DB.SaveChanges();
            }
        }

        public string GetWarrantyVerbose(int RequestId)
        {
            var warranty = DB.Warranties.FirstOrDefault(w => w.RequestId == RequestId);
            if (warranty != null)
            {
                if (warranty.InWarranty)
                {
                    if (GetRequest(RequestId).Status == RequestFlowStatus.Received) return "To be confirmed";
                    return "Yes";
                }
                return "Denied";
            }
            return "No";
        }

        //public WarehouseStatusFlow CreateWarehouseFlow(int requestId, RequestFlowStatus status,
        //    string originId,
        //    string userId, string description)
        //{
        //    var refurb = DB.RefurbRequests.Find(requestId);


        //    var flow = DB.WarehouseStatusFlows.Add(new WarehouseStatusFlow {
        //        RequestId = refurb.Id,
        //        RequestFlowStatus = status, 
        //        OriginId = originId,
        //        DestinationId = destinationId,
        //        UserId = userId,
        //        Date = DateTime.Now,
        //        Description = description
        //    });
        //    DB.SaveChanges();

        //    return flow;
        //} 

        public void Dispose()
        {
            DB.Dispose();
        }
    }
}
