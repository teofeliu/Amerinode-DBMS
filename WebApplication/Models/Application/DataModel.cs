namespace WebApplication.Models.Application
{
    using Microsoft.AspNet.Identity.EntityFramework;
    using System;
    using System.Data.Entity;
    using System.Data.Entity.ModelConfiguration.Conventions;
    using System.Linq;
    using WebApplication.Models.Auth;

    public class DataModel : DbContext
    {
        // Your context has been configured to use a 'DataModel' connection string from your application's 
        // configuration file (App.config or Web.config). By default, this connection string targets the 
        // 'AmerinodeWebsite.Models.DataModel' database on your LocalDb instance. 
        // 
        // If you wish to target a different database and/or database provider, modify the 'DataModel' 
        // connection string in the application configuration file.
        public DataModel()
            : base("DataModel")
        {
        }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Conventions.Remove<OneToManyCascadeDeleteConvention>();
            modelBuilder.Conventions.Remove<ManyToManyCascadeDeleteConvention>();
        }

        // Add a DbSet for each entity type that you want to include in your model. For more information 
        // on configuring and using a Code First model, see http://go.microsoft.com/fwlink/?LinkId=390109.

        public virtual DbSet<Manufacturer> Manufacturers { get; set; }
        public virtual DbSet<ModelType> ModelTypes { get; set; }
        public virtual DbSet<RequestFlow> RequestFlows { get; set; }
        public virtual DbSet<Model> Models { get; set; }
        public virtual DbSet<Supply> Supplies { get; set; }
        public virtual DbSet<Warranty> Warranties { get; set; }
        public virtual DbSet<Contact> Contacts { get; set; }
        public virtual DbSet<RepairType> RepairTypes { get; set; }
        public virtual DbSet<RefurbRequest> RefurbRequests { get; set; }
        public virtual DbSet<HardwareOverview> HardwareOverviews { get; set; }
        public virtual DbSet<Batch> Batches { get; set; }
        public virtual DbSet<MissingChecklist> MissingChecklists { get; set; }        
        public virtual DbSet<CosmeticOverview> CosmeticOverviews { get; set; }
        public virtual DbSet<CosmeticChecklist> CosmeticChecklists { get; set; }
        public virtual DbSet<Trial> Trials { get; set; }
        public virtual DbSet<RepairRepairType> TrialRepairTypes { get; set; }
        public virtual DbSet<Cosmetic> Cosmetics { get; set; }
        public virtual DbSet<Repair> Repairs { get; set; }
        public virtual DbSet<Scrap> Scraps { get; set; }
        public virtual DbSet<FinalInspection> FinalInspections { get; set; }
        public virtual DbSet<FunctionalTest> FunctionalTests { get; set; }
        public virtual DbSet<TrialFunctionalTest> TrialFunctionalTests { get; set; }
        public virtual DbSet<PermissionResource> PermissionResources { get; set; }
        public virtual DbSet<PermissionOperation> PermissionOperations { get; set; }
        public virtual DbSet<PermissionRole> PermissionsRoles { get; set; }
        public virtual DbSet<PermissionUser> PermissionsUser { get; set; }
        public virtual DbSet<CosmeticStatus> CosmeticStatuses { get; set; }
        public virtual DbSet<BatchItem> BatchItems { get; set; }
        public virtual DbSet<Delivery> Deliveries { get; set; }
        public virtual DbSet<DeliveryBatch> DeliveryBatches { get; set; }
        public virtual DbSet<DeliveryBatchItem> DeliveryBatchItems { get; set; }
        public virtual DbSet<ScrapBatch> ScrapBatches { get; set; }
        public virtual DbSet<ScrapBatchItem> ScrapBatchItems { get; set; }
        public virtual DbSet<Warehouse> Warehouses { get; set; }
        public virtual DbSet<WarehouseRequestStatus> WarehouseRequestStatuses { get; set; }
        public virtual DbSet<WarehouseStatusFlow> WarehouseStatusFlows { get; set; }
        public virtual DbSet<DOA> DOAs { get; set; }
        public virtual DbSet<Parameters> Parameters { get; set; }
        public virtual DbSet<BgaScrap> BgaScraps { get; set; }
        public virtual DbSet<BgaScrapBatch> BgaScrapBatches { get; set; }
        public virtual DbSet<BgaScrapBatchItem> BgaScrapBatchItems { get; set; }
        //public virtual DbSet<BatchStock> BatchStock { get; set; }

        public System.Data.Entity.DbSet<WebApplication.Models.Application.BatchStock> BatchStock { get; set; }


        public System.Data.Entity.DbSet<WebApplication.Models.Application.BatchProducts> BatchProducts { get; set; }

        public System.Data.Entity.DbSet<WebApplication.Models.Application.BatchOrder> BatchOrders { get; set; }
    }
}
