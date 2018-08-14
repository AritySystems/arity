using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace AritySystems.Models
{
    public class SupplierCartonDetailModel
    {
        public int Id { get; set; }
        public int SupplierAssignedMapId { get; set; }
        public string Product_English_Name { get; set; }
        public string Product_Chinese_Name { get; set; }
        public decimal PcsPerCartoon { get; set; }
        public int TotalCartoons { get; set; }
        public decimal NetWeight { get; set; }
        public decimal TotalNetWeight { get; set; }
        public decimal GrossWeight { get; set; }
        public decimal TotalGrossWeight { get; set; }
        public decimal CartoonSize { get; set; }
        public decimal CartoonBM { get; set; }
        public string CartoonNumber { get; set; }
        public int Status { get; set; }
    }
    public class SupplierOrderItemAdd
    {
        public string OrderLineItemId { get; set; }
        public string OldQuantity { get; set; }
        public string NewQuantity { get; set; }
        public string SupplierId { get; set; }
    }
}