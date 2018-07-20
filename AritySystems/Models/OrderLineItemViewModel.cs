using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace AritySystems.Models
{
     //{ "data": "Order_Name" },
     //               { "data": "Product_Name" },
     //               { "data": "Purchase_Price_dollar" },
     //               { "data": "Sales_Price_dollar" },
     //               { "data": "Purchase_Price_rmb" },
     //               { "data": "Sales_Price_rmb" },
     //               { "data": "quantity" },
    public class OrderLineItemViewModel
    {
        public int Id { get; set; }

        public int OrderId { get; set; }

        public int ProductId { get; set; }
        public string Order_Name { get; set; }
        public string Product_Name { get; set; }
        public decimal Purchase_Price_dollar { get; set; }
        public decimal Sales_Price_dollar { get; set; }
        public decimal Purchase_Price_rmb { get; set; }
        public decimal Sales_Price_rmb { get; set; }
        public decimal quantity { get; set; }

        public List<SelectListItem> Suppliers { get; set; }
        public string CreatedDate { get; set; }
        public string ModifiedDate { get; set; }
    }
}