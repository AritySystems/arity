using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace AritySystems.Models
{
    public class ProductDetails
    {
        public int Id { get; set; }

        public string Chinese_Name { get; set; }

        public string English_Name { get; set; }

        public decimal Quantity { get; set; }

        public decimal Dollar_Price { get; set; }

        public decimal RMB_Price { get; set; }

        public string Unit { get; set; }

        public string Description { get; set; }

        public DateTime? ModifiedDate { get; set; }
    }
}