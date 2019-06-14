using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace AritySystems.Models
{
    public class SupplierOrderLineItemModel
    {
        public int Id { get; set; }
        public int? SupplierId { get; set; }
        public string SupplierName { get; set; }
        public string Order_Prefix { get; set; }
      
        public Nullable<int> OrderSupplierMapId { get; set; }
        public int Status { get; set; }
        public decimal Quantity { get; set; }
        public string ModifiedDate { get; set; }
        public string CreatedDate { get; set; }

        public int OrderId { get; set; }
    }
}