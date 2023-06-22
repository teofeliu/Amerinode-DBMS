using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Web;

namespace WebApplication.Models.Application
{
    public enum WarehouseType
    {
        [Description("Origin Warehouse to receive devices")]
        Origin = 1,
        [Description("Warehouses where devices are worked in")]
        Warehouse, 
        [Description("Destination warehouses to delivery devices")]
        Destination
    }
}