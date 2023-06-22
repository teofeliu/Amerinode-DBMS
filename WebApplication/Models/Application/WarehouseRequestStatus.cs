using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebApplication.Models.Application
{
    /// <summary>
    /// This model supply the association with warehouses rules.
    /// All status must have an automatic warehouse allocation. So, this model stores
    /// the status changed and the new place where device will be rellocated.
    /// </summary>
    public class WarehouseRequestStatus
    {
        [Key]
        [Required]
        [DisplayName("ID")]
        public int Id { get; set; }

        [Required]
        [DisplayName("Target Warehouse")]
        [Index("IX__WarehouseReqStatus", 1, IsUnique = false)]
        public string WarehouseId { get; set; }

        [ForeignKey("WarehouseId")]
        public virtual Warehouse Warehouse { get; set; }

        [Required]
        [DisplayName("Target status")]
        [Index("UQ__WarehStatus", IsUnique = true)]
        [Index("IX__WarehouseReqStatus", 2, IsUnique = false)]
        public RequestFlowStatus RequestFlowStatus { get; set; }
    }
}