using System;
using System.Collections.Generic;
using System.Linq;
using NPOI.XSSF.UserModel;
using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using System.IO;
using System.Linq.Dynamic;
using System.Security.Principal;
using System.Data.Entity;
using WebApplication.Extensions;

namespace WebApplication.Models.Application
{
    public class BatchManager
    {
        DataModel DB = new DataModel();

        private DataFormatter _formatter = new DataFormatter();

        public BatchManager() { }

        public Batch DeleteBatch(int id)
        {
            var batch = DB.Batches.First(b => b.Id == id
                && b.Status != BatchStatus.Deleted);

            foreach (var refurb in
                DB.RefurbRequests
                    .Where(rr => rr.BatchId == id && !rr.Cancelled))
            {
                refurb.Cancelled = true;
                DB.Entry(refurb).State = EntityState.Modified;
            }

            batch.Status = BatchStatus.Deleted;
            DB.Entry(batch).State = EntityState.Modified;
            DB.SaveChanges();

            return batch;
        }

        public Batch GetBatch(int id)
        {
            var r = DB.Batches.Find(id);
            r.BatchItems = GetBatchItems(id);
            r.BatchItemsConferred = GetBatchItemsConferred(id);
            r.BatchItemsNotConferred = GetBatchItemsNotConferred(id);
            r.BatchItemsFound = GetBatchItemsFound(id);
            r.BatchItemsNotFound = GetBatchItemsNotFound(id);
            return r;
        }

        public BatchItem GetBatchItemBySN(int batchId, string sn)
        {
            var bi = (from m in DB.BatchItems
                      where m.BatchId == batchId && m.SerialNumber == sn
                      select m).FirstOrDefault();
            return bi;
        }

      
        public IEnumerable<BatchStock> BatchStockGetAll(string nRequest)
        {
             var bi = (from m in DB.BatchStock
                       where m.NumeroNota == nRequest
                       select m).ToList();
            return bi;


        }
        public IEnumerable<BatchProducts> BatchProductsGet(int nRequest)
        {
            var bi = (from m in DB.BatchProducts
                      where m.BatchStock.Id == nRequest
                      select m).ToList();
            return bi;


        }

        public BatchStock GetBatchStock(string numeroNota)
        {
            var bi = (from m in DB.BatchStock
                      where m.NumeroNota == numeroNota
                      select m).FirstOrDefault();
            return bi;
        }

        public IEnumerable<Model> GetModelIdFilterStock()
        {
            var bi = (from m in DB.Models
                      where m.Type.Controlstock != false && m.Stock > 0
                      select m).ToList();
            return bi;
        }

        public IEnumerable<Model> GetModelIdFilterControlStock()
        {
            var bi = (from m in DB.Models
                      where m.Type.Controlstock != false
                      select m).ToList();
            return bi; ;
        }





        public BatchItem GetBatchItemById(int batchItemId)
        {
            var bi = (from m in DB.BatchItems
                      where m.Id == batchItemId
                      select m).FirstOrDefault();
            return bi;
        }

        public BatchItem GetBatchItemById(int batchId, int batchItemId)
        {
            var bi = (from m in DB.BatchItems
                      where m.BatchId == batchId && m.Id == batchItemId
                      select m).FirstOrDefault();
            return bi;
        }

        public ICollection<BatchItem> GetBatchItemsFound(int batchId)
        {
            return (from m in DB.BatchItems
                    where m.BatchId == batchId && m.Conferred == true && m.Found == true
                    select m).ToList();
        }


        public ICollection<BatchItem> GetBatchItemsNotFound(int batchId)
        {
            return (from m in DB.BatchItems
                    where m.BatchId == batchId && m.Conferred == true && m.Found == false
                    select m).ToList();
        }


        public ICollection<BatchItem> GetBatchItemsConferred(int batchId)
        {
            return (from m in DB.BatchItems
                    where m.BatchId == batchId && m.Conferred == true
                    select m).ToList();
        }

        public ICollection<BatchItem> GetBatchItems(int batchId)
        {
            return (from m in DB.BatchItems
                    where m.BatchId == batchId
                    select m).ToList();
        }


        public ICollection<BatchItem> GetBatchItemsNotConferred(int batchId)
        {
            return (from m in DB.BatchItems
                    where m.BatchId == batchId && m.Conferred == false
                    select m).ToList();
        }

        public BatchItem VerifySerialNumber(int batchId, string sn)
        {
            var b = GetBatch(batchId);
            var bi = DB.BatchItems.Where(x => x.BatchId == batchId && x.SerialNumber == sn).FirstOrDefault();

            if (bi != null && bi.Conferred) return bi; //já conferido

            if (bi == null)
            {
                b.Status = BatchStatus.Invalid;
                bi = new BatchItem();
                bi.BatchId = batchId;
                bi.SerialNumber = sn;
                bi.Found = false;
                DB.BatchItems.Add(bi);
            }
            else
            {
                bi.Found = true;
            }

            bi.Conferred = true;
            bi.DateConferred = DateTime.Now;

            DB.SaveChanges();
            return bi;

        }



        public List<Batch> DivergentBatches()
        {
            var ret = from r in DB.Batches
                      where r.Quantity != r.QuantityConferred
                      select r;
            return ret.ToList();
        }

        public List<Batch> DivergentBatchesDash()
        {
            var ret = from r in DB.Batches
                      where r.Quantity != r.QuantityConferred && r.Status == BatchStatus.PendingReview
                      select r;
            return ret.ToList();
        }

        /*Listas Por Status*/
        public List<Batch> BatchesByStatus(BatchStatus Status)
        {
            var ret = from r in DB.Batches
                      where r.Status == Status
                      select r;
            return ret.ToList();
        }

        public List<Batch> TestedBatches()
        {
            var l = from r in DB.Batches
                    where r.Status == BatchStatus.Tested
                    select r;
            return l.ToList();
        }

        public List<Batch> ConferredBatches()
        {
            var l = from r in DB.Batches
                    where r.Status == BatchStatus.Conferred
                    select r;
            return l.ToList();
        }

        public List<Batch> PendingReviewBatches()
        {
            var l = from r in DB.Batches
                    where r.Status == BatchStatus.PendingReview
                    select r;
            return l.ToList();
        }

        public List<BatchProducts> BatchProductsGetList()
        {
            var l = from r in DB.BatchProducts
                    where r.BatchStock.NumeroNota == "2"
                    select r;
            return l.ToList();
        }


        public ModelType GetOrCreateModelType(string name)
        {
            var modelType = DB.ModelTypes.Where(x => x.Name == name).FirstOrDefault();
            if (modelType == null)
            {
                modelType = new ModelType();
                modelType.Name = name;
                DB.ModelTypes.Add(modelType);
                DB.SaveChanges();
            }

            return modelType;
        }

        public Model GetOrCreateModel(string tm, string name, int modelTypeId)
        {
            var model = DB.Models.Where(x => x.TM == tm).FirstOrDefault();
            if (model == null)
            {
                model = new Model();
                model.TM = tm;
                model.Name = name;
                model.ModelTypeId = modelTypeId;
                DB.Models.Add(model);
                DB.SaveChanges();
            }
            return model;

        }

        public Warehouse GetOrCreateWarehouse(string id, string name)
        {
            if (String.IsNullOrEmpty(id))
            {
                throw new InvalidOperationException("You cannot import a worksheet without " +
                    "origin and destination warehouses");
            }

            // cleaning the warehouse code
            int val;
            var parsed = int.TryParse(id, out val);
            if (parsed) { id = val.ToString(); }

            var model = DB.Warehouses.FirstOrDefault(w => w.Id == id);
            if (model == null)
            {
                throw new KeyNotFoundException(String.Format("The warehouse with ID {0} was not found. " +
                    "Please, review your woorksheet and try again.", id));


                //model = DB.Warehouses
                //    .Add(new Warehouse
                //    {
                //        Id = id,
                //        Name = name
                //    });
                //DB.SaveChanges();
            }

            return model;
        }

        public BatchItem GetOrCreateBatchItem(int batchId, int modelId, string sn,
            Warehouse origin, Warehouse destination, string invoice)
        {
            var b = new BatchItem();
            b.BatchId = batchId;
            b.ModelId = modelId;
            b.SerialNumber = sn;
            b.Origin = origin;
            b.Destination = destination;
            b.Invoice = invoice;
            DB.BatchItems.Add(b);
            DB.SaveChanges();
            return b;

        }

        public Boolean DeleteBatchItem(int batchId, int modelId)
        {
            var model = this.GetBatchItemById(batchId, modelId);
            if (model != null)
            {
                DB.BatchItems.Remove(model);
                DB.SaveChanges();
                return true;
            }

            return false;
        }

        #region Import
        public void CreateBatchFromExcelStream(Stream stream, String extension, String userId)
        {
            ISheet sheet;
            IWorkbook wb;
            if (extension == ".xlsx") wb = new XSSFWorkbook(stream); else wb = new HSSFWorkbook(stream);
            sheet = wb.GetSheetAt(0);

            /**
             * Bloqueio para não importar um serial number que tiver status ativo no sistema
             * 
             * adicionada a validaçao para não permitir carga com SN em garantia
             */
            var requestManager = new RequestManager();
            var daysParam = DB.Parameters.FirstOrDefault(p => p.Name == "batch.import.daysToBlockImportByWarranty");
            int days = Int32.Parse(daysParam.Value);

            for (int row = 1; row <= sheet.LastRowNum; row++)
            {
                var sheetRow = sheet.GetRow(row);
                if (sheetRow != null)
                {
                    requestManager.ValidateSNStillProcessing(sheetRow.GetCell(3).GetGenericValue(_formatter));
                    requestManager.ValidateSNInWarranty(sheetRow.GetCell(3).GetGenericValue(_formatter), days);
                }
            }
            /** eof Bloqueio para não importar um serial number que tiver status ativo no sistema */

            var batch = new Batch(userId);
            batch.Quantity = sheet.LastRowNum;
            DB.Batches.Add(batch);
            DB.SaveChanges();

            // row = 1; ignora a primeira linha
            for (int row = 1; row <= sheet.LastRowNum; row++)
            {
                var sheetRow = sheet.GetRow(row);

                if (sheetRow != null)
                {
                    var tm = sheetRow.GetCell(0).GetGenericValue(_formatter);
                    var nome = sheetRow.GetCell(1).GetGenericValue(_formatter);
                    var tipo = sheetRow.GetCell(2).GetGenericValue(_formatter);
                    var sn = sheetRow.GetCell(3).GetGenericValue(_formatter);

                    var modelType = GetOrCreateModelType(tipo);
                    var model = GetOrCreateModel(tm, nome, modelType.Id);

                    ICell originCell = sheetRow.GetCell(4);
                    var origin = GetOrCreateWarehouse(
                        (originCell.CellType == CellType.String) 
                        ? originCell.StringCellValue 
                        : originCell.NumericCellValue.ToString(),
                        sheetRow.GetCell(5).StringCellValue);

                    Warehouse destination;
                    var warehouse = DB.WarehouseRequestStatuses
                        .FirstOrDefault(w => w.RequestFlowStatus == RequestFlowStatus.Received);
                    if(warehouse == null)
                    {
                        destination = GetOrCreateWarehouse("88", "LIVE - Em Cosme");
                    } else
                    {
                        destination = warehouse.Warehouse;
                    }

                    var invoice = sheetRow.GetCell(8).GetGenericValue(_formatter);

                    var batchItem = GetOrCreateBatchItem(batch.Id, model.Id, sn, origin, destination, invoice);

                }
            }

            DB.SaveChanges();


        }
        #endregion

        #region Delivery
        public List<DeliveryBatch> GetDeliveryBatches()
        {
            return DB.DeliveryBatches
                .OrderBy(d => (d.Delivered == null) ? d.Delivered : d.Created)
                .ToList();
        }

        public DeliveryBatch GetOrCreateDeliveryBatch(int? id, IPrincipal user)
        {
            DeliveryBatch deliveryBatch;
            if (id == null)
            {
                deliveryBatch = new DeliveryBatch
                {
                    UserId = user.Identity.Name,
                    Status = DeliveryBatchStatus.Open,
                    Created = DateTime.Now
                };
                DB.DeliveryBatches.Add(deliveryBatch);
                DB.SaveChanges();

                deliveryBatch.Items = DB.DeliveryBatchItems.Where(d => d.DeliveryBatchId == id).ToList();

                return deliveryBatch;
            }

            deliveryBatch = DB.DeliveryBatches.Find(id);
            deliveryBatch.Items = DB.DeliveryBatchItems.Where(d => d.DeliveryBatchId == id).ToList();

            return deliveryBatch;
        }

        public bool PerformCancelDeliveryBatch(int id, IPrincipal user)
        {
            var deliveryBatch = DB.DeliveryBatches.Find(id);
            if (deliveryBatch != null)
            {
                deliveryBatch.Status = DeliveryBatchStatus.Cancelled;
                deliveryBatch.Cancelled = DateTime.Now;
                deliveryBatch.UserCancelId = user.Identity.Name;

                DB.Entry(deliveryBatch).State = EntityState.Modified;
                DB.SaveChanges();

                return true;
            }
            return false;
        }

        public DeliveryBatchItemResult AddDeliveryBatchItem(int deliveryBatchId, string sn, IPrincipal user)
        {
            var refurb = DB.RefurbRequests
                .Where(rr => rr.SerialNumber == sn && rr.Status == RequestFlowStatus.FinalInspection)
                .FirstOrDefault();

            var result = new DeliveryBatchItemResult() { State = "nothing" };

            if (refurb != null)
            {
                var item = DB.DeliveryBatchItems
                    .Where(dbi => dbi.DeliveryBatchId == deliveryBatchId && dbi.RefurbRequestId == refurb.Id)
                    .FirstOrDefault();

                if (item != null)
                {
                    result.Message = "Refurb request already in this batch";
                    result.Item = item.ToJson();
                    result.State = "alreadyExists";
                }
                else
                {
                    item = new DeliveryBatchItem
                    {
                        DeliveryBatchId = deliveryBatchId,
                        Date = DateTime.Now,
                        RefurbRequestId = refurb.Id,
                        UserId = user.Identity.Name,
                        DestinationId = refurb.DestinationId,
                        OriginId = refurb.WarehouseId
                    }; 
                    item = DB.DeliveryBatchItems.Add(item);
                    DB.SaveChanges();

                    result.Message = "Refurb request added to this batch";
                    result.State = "added";
                    item = DB.DeliveryBatchItems
                            .Include("Origin")
                            .Include("Destination")
                            .First(dbi => dbi.Id == item.Id);
                    result.Item = item.ToJson();
                }
            }
            else
            {
                result.Message = "Serial number not found on final inspection status";
                result.State = "notFound";
            }

            result.Count = DB.DeliveryBatchItems.Count(dbi => dbi.DeliveryBatchId == deliveryBatchId);

            return result;
        }

        public DeliveryBatchItemResult RemoveDeliveryBatchItem(int deliveryBatchItemId)
        {
            var result = new DeliveryBatchItemResult() { State = "nothing" };

            var item = DB.DeliveryBatchItems.Find(deliveryBatchItemId);
            if (item != null)
            {
                result.Item = item.ToJson();
                try
                {
                    var deleted = DB.DeliveryBatchItems.Remove(item);
                    DB.SaveChanges();

                    result.Message = "Delivery batch item removed";
                    result.State = "removed";
                }
                catch (Exception)
                {
                    result.Message = "Unable to delete item";
                }
            }

            result.Count = DB.DeliveryBatchItems.Count(d => d.DeliveryBatchId == item.DeliveryBatchId);

            return result;
        }

        public DeliveryBatch PerformDeliveryBatch(int deliveryBatchId, IPrincipal user, DateTime? deliveryDate)
        {
            var batch = DB.DeliveryBatches.Find(deliveryBatchId);
            if (batch.Status != DeliveryBatchStatus.Open)
            {
                throw new InvalidOperationException("Delivery batch already proccessed");
            }

            var items = DB.DeliveryBatchItems.Where(d => d.DeliveryBatchId == batch.Id).ToList();

            foreach (var item in items)
            {
                DB.Deliveries.Add(new Delivery
                {
                    RequestId = item.RefurbRequest.Id,
                    Date = deliveryDate ?? DateTime.Now,
                    UserId = user.Identity.Name,
                    DeliveryBatchId = batch.Id,
                    Description = String.Format("Created from batch {0}", batch.GetCode)
                });

                DB.RequestFlows.Add(new RequestFlow
                {
                    DateRequested = deliveryDate ?? DateTime.Now,
                    Description = String.Format("Created from batch {0}", batch.GetCode),
                    RequestId = item.RefurbRequest.Id,
                    UserId = user.Identity.Name,
                    Status = RequestFlowStatus.Delivered
                });

                item.RefurbRequest.Status = RequestFlowStatus.Delivered;
                item.RefurbRequest.LastUpdated = deliveryDate ?? DateTime.Now;

                var warehouse = DB.WarehouseRequestStatuses
                    .FirstOrDefault(w => w.RequestFlowStatus == RequestFlowStatus.Delivered);
                if(warehouse != null)
                {
                    item.RefurbRequest.DestinationId = warehouse.WarehouseId;
                    item.RefurbRequest.DateDestination = DateTime.Now;
                }

            }

            batch.Status = DeliveryBatchStatus.Delivered;
            batch.Delivered = deliveryDate ?? DateTime.Now;
            batch.UserDeliveryId = user.Identity.Name;

            DB.SaveChanges();

            return batch;
        }

        #endregion

        #region Scrap

        public ScrapBatch GetOrCreateScrapBatch(int? id, IPrincipal user)
        {
            ScrapBatch scrapBatch;
            if (id == null)
            {
                scrapBatch = new ScrapBatch
                {
                    UserId = user.Identity.Name,
                    Status = ScrapBatchStatus.Open,
                    Created = DateTime.Now
                };
                DB.ScrapBatches.Add(scrapBatch);
                DB.SaveChanges();

                scrapBatch.Items = DB.ScrapBatchItems.Where(d => d.ScrapBatchId == id).ToList();

                return scrapBatch;
            }

            scrapBatch = DB.ScrapBatches.Find(id);
            scrapBatch.Items = DB.ScrapBatchItems.Where(d => d.ScrapBatchId == id).ToList();

            return scrapBatch;
        }

        public ScrapBatchItemResult AddScrapBatchItem(int scrapBatchId, string sn, IPrincipal user)
        {
            var refurb = DB.RefurbRequests
                .Where(rr => rr.SerialNumber == sn
                    && rr.Status == RequestFlowStatus.SentToScrapEvaluation)
                .FirstOrDefault();

            var result = new ScrapBatchItemResult() { State = "nothing" };

            if (refurb != null)
            {
                var item = DB.ScrapBatchItems
                    .Where(sbi => sbi.ScrapBatchId == scrapBatchId && sbi.RefurbRequestId == refurb.Id)
                    .FirstOrDefault();

                if (item != null)
                {
                    result.Message = "Refurb request already in this batch";
                    result.Item = item.ToJson();
                    result.State = "alreadyExists";
                }
                else
                {
                    item = new ScrapBatchItem
                    {
                        ScrapBatchId = scrapBatchId,
                        Date = DateTime.Now,
                        RefurbRequestId = refurb.Id,
                        UserId = user.Identity.Name,
                        DestinationId = refurb.DestinationId,
                        OriginId = refurb.WarehouseId
                    };
                    item = DB.ScrapBatchItems.Add(item);
                    DB.SaveChanges();

                    result.Message = "Refurb request added to this batch";
                    result.State = "added";
                    item = DB.ScrapBatchItems
                        .Include("Origin")
                        .Include("Destination")
                        .First(sbi => sbi.Id == item.Id);

                    result.Item = item.ToJson();
                }
            }
            else
            {
                result.Message = "Serial number not found on sent to scrap evaluation status";
                result.State = "notFound";
            }

            result.Count = DB.ScrapBatchItems.Count(sbi => sbi.ScrapBatchId == scrapBatchId);

            return result;
        }

        public ScrapBatchItemResult RemoveScrapBatchItem(int scrapBatchItemId)
        {
            var result = new ScrapBatchItemResult() { State = "nothing" };

            var item = DB.ScrapBatchItems.Find(scrapBatchItemId);
            if (item != null)
            {
                result.Item = item.ToJson();
                try
                {
                    var deleted = DB.ScrapBatchItems.Remove(item);
                    DB.SaveChanges();

                    result.Message = "Scrap batch item removed";
                    result.State = "removed";
                }
                catch (Exception)
                {
                    result.Message = "Unable to delete item";
                }
            }

            result.Count = DB.ScrapBatchItems.Count(d => d.ScrapBatchId == item.ScrapBatchId);

            return result;
        }

        public bool PerformCancelScrapBatch(int id, IPrincipal user)
        {
            var scrapBatch = DB.ScrapBatches.Find(id);
            if (scrapBatch != null)
            {
                scrapBatch.Status = ScrapBatchStatus.Cancelled;
                scrapBatch.Cancelled = DateTime.Now;
                scrapBatch.UserCancelId = user.Identity.Name;

                DB.Entry(scrapBatch).State = EntityState.Modified;
                DB.SaveChanges();

                return true;
            }
            return false;
        }


        public ScrapBatch PerformScrapBatch(int scrapBatchId, IPrincipal user, DateTime? deliveryDate)
        {
            var batch = DB.ScrapBatches.Find(scrapBatchId);
            if (batch.Status != ScrapBatchStatus.Open)
            {
                throw new InvalidOperationException("Scrap batch already proccessed");
            }

            var items = DB.ScrapBatchItems.Where(sb => sb.ScrapBatchId == batch.Id).ToList();

            foreach (var item in items)
            {
                DB.Scraps.Add(new Scrap
                {
                    RequestId = item.RefurbRequest.Id,
                    Date = deliveryDate ?? DateTime.Now,
                    UserId = user.Identity.Name,
                    ScrapBatchId = batch.Id,
                    Description = String.Format("Created from batch {0}", batch.GetCode)
                });

                DB.RequestFlows.Add(new RequestFlow
                {
                    DateRequested = deliveryDate ?? DateTime.Now,
                    Description = String.Format("Created from batch {0}", batch.GetCode),
                    RequestId = item.RefurbRequest.Id,
                    UserId = user.Identity.Name,
                    Status = RequestFlowStatus.SentToScrap
                });

                item.RefurbRequest.Status = RequestFlowStatus.SentToScrap;
                item.RefurbRequest.LastUpdated = deliveryDate ?? DateTime.Now;

                var warehouse = DB.WarehouseRequestStatuses
                    .FirstOrDefault(w => w.RequestFlowStatus == RequestFlowStatus.SentToScrap);
                if(warehouse != null)
                {
                    item.RefurbRequest.DestinationId = warehouse.WarehouseId;
                    item.RefurbRequest.DateDestination = DateTime.Now;
                }
            }

            batch.Status = ScrapBatchStatus.Delivered;
            batch.Delivered = deliveryDate ?? DateTime.Now;
            batch.UserDeliveryId = user.Identity.Name;

            DB.SaveChanges();

            return batch;
        }
        #endregion

        #region BgaScrap

        public BgaScrapBatch GetOrCreateBgaScrapBatch(int? id, IPrincipal user)
        {
            BgaScrapBatch scrapBatch;
            if (id == null)
            {
                scrapBatch = new BgaScrapBatch
                {
                    UserId = user.Identity.Name,
                    Status = ScrapBatchStatus.Open,
                    Created = DateTime.Now
                };
                DB.BgaScrapBatches.Add(scrapBatch);
                DB.SaveChanges();

                scrapBatch.Items = DB.BgaScrapBatchItems.Where(d => d.BgaScrapBatchId == id).ToList();

                return scrapBatch;
            }

            scrapBatch = DB.BgaScrapBatches.Find(id);
            scrapBatch.Items = DB.BgaScrapBatchItems.Where(d => d.BgaScrapBatchId == id).ToList();

            return scrapBatch;
        }

        public ScrapBatchItemResult AddBgaScrapBatchItem(int scrapBatchId, string sn, IPrincipal user)
        {
            var refurb = DB.RefurbRequests
                .Where(rr => rr.SerialNumber == sn
                    && rr.Status == RequestFlowStatus.SentToBgaScrapEvaluation)
                .FirstOrDefault();

            var result = new ScrapBatchItemResult() { State = "nothing" };

            if (refurb != null)
            {
                var item = DB.BgaScrapBatchItems
                    .Where(sbi => sbi.BgaScrapBatchId == scrapBatchId && sbi.RefurbRequestId == refurb.Id)
                    .FirstOrDefault();

                if (item != null)
                {
                    result.Message = "Refurb request already in this batch";
                    result.Item = item.ToJson();
                    result.State = "alreadyExists";
                }
                else
                {
                    item = new BgaScrapBatchItem
                    {
                        BgaScrapBatchId = scrapBatchId,
                        Date = DateTime.Now,
                        RefurbRequestId = refurb.Id,
                        UserId = user.Identity.Name,
                        DestinationId = refurb.DestinationId,
                        OriginId = refurb.WarehouseId
                    };
                    item = DB.BgaScrapBatchItems.Add(item);
                    DB.SaveChanges();

                    result.Message = "Refurb request added to this batch";
                    result.State = "added";
                    item = DB.BgaScrapBatchItems
                        .Include("Origin")
                        .Include("Destination")
                        .First(sbi => sbi.Id == item.Id);

                    result.Item = item.ToJson();
                }
            }
            else
            {
                result.Message = "Serial number not found on sent to scrap evaluation status";
                result.State = "notFound";
            }

            result.Count = DB.BgaScrapBatchItems.Count(sbi => sbi.BgaScrapBatchId == scrapBatchId);

            return result;
        }

        public ScrapBatchItemResult RemoveBgaScrapBatchItem(int scrapBatchItemId)
        {
            var result = new ScrapBatchItemResult() { State = "nothing" };

            var item = DB.BgaScrapBatchItems.Find(scrapBatchItemId);
            if (item != null)
            {
                result.Item = item.ToJson();
                try
                {
                    var deleted = DB.BgaScrapBatchItems.Remove(item);
                    DB.SaveChanges();

                    result.Message = "BGA Scrap batch item removed";
                    result.State = "removed";
                }
                catch (Exception)
                {
                    result.Message = "Unable to delete item";
                }
            }

            result.Count = DB.BgaScrapBatchItems.Count(d => d.BgaScrapBatchId == item.BgaScrapBatchId);

            return result;
        }

        public bool PerformCancelBgaScrapBatch(int id, IPrincipal user)
        {
            var scrapBatch = DB.BgaScrapBatches.Find(id);
            if (scrapBatch != null)
            {
                scrapBatch.Status = ScrapBatchStatus.Cancelled;
                scrapBatch.Cancelled = DateTime.Now;
                scrapBatch.UserCancelId = user.Identity.Name;

                DB.Entry(scrapBatch).State = EntityState.Modified;
                DB.SaveChanges();

                return true;
            }
            return false;
        }


        public BgaScrapBatch PerformBgaScrapBatch(int scrapBatchId, IPrincipal user, DateTime? deliveryDate)
        {
            var batch = DB.BgaScrapBatches.Find(scrapBatchId);
            if (batch.Status != ScrapBatchStatus.Open)
            {
                throw new InvalidOperationException("BGA Scrap batch already proccessed");
            }

            var items = DB.BgaScrapBatchItems.Where(sb => sb.BgaScrapBatchId == batch.Id).ToList();

            foreach (var item in items)
            {
                DB.BgaScraps.Add(new BgaScrap
                {
                    RequestId = item.RefurbRequest.Id,
                    Date = deliveryDate ?? DateTime.Now,
                    UserId = user.Identity.Name,
                    ScrapBatchId = batch.Id,
                    Description = String.Format("Created from batch {0}", batch.GetCode)
                });

                DB.RequestFlows.Add(new RequestFlow
                {
                    DateRequested = deliveryDate ?? DateTime.Now,
                    Description = String.Format("Created from batch {0}", batch.GetCode),
                    RequestId = item.RefurbRequest.Id,
                    UserId = user.Identity.Name,
                    Status = RequestFlowStatus.SentToBgaScrap
                });

                item.RefurbRequest.Status = RequestFlowStatus.SentToBgaScrap;
                item.RefurbRequest.LastUpdated = deliveryDate ?? DateTime.Now;

                var warehouse = DB.WarehouseRequestStatuses
                    .FirstOrDefault(w => w.RequestFlowStatus == RequestFlowStatus.SentToBgaScrap);
                if (warehouse != null)
                {
                    item.RefurbRequest.DestinationId = warehouse.WarehouseId;
                    item.RefurbRequest.DateDestination = DateTime.Now;
                }
            }

            batch.Status = ScrapBatchStatus.Delivered;
            batch.Delivered = deliveryDate ?? DateTime.Now;
            batch.UserDeliveryId = user.Identity.Name;

            DB.SaveChanges();

            return batch;
        }
        #endregion


        public void Dispose()
        {
            DB.Dispose();
        }
    }

    public class DeliveryBatchItemResult
    {
        public string State { get; set; }
        public string Message { get; set; }
        public int Count { get; set; }
        public IDictionary<string, object> Item { get; set; }
    }

    public class ScrapBatchItemResult
    {
        public string State { get; set; }
        public string Message { get; set; }
        public int Count { get; set; }
        public IDictionary<string, object> Item { get; set; }
    }
}