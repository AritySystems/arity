using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace AritySystems.Models
{
    public class SupplierOrderLineItemModel
    {
        public int Id { get; set; }
        public Nullable<int> SupplierId { get; set; }
        public string SupplierName { get; set; }
        public string Order_Prefix { get; set; }
      
        public Nullable<int> OrderSupplierMapId { get; set; }
        public string Status { get; set; }
        public Nullable<decimal> Quantity { get; set; }
        public string ModifiedDate { get; set; }
        public string CreatedDate { get; set; }
    }
}