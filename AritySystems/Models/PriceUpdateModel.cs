using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace AritySystems.Models
{
    public class PriceUpdateModel
    {
        public int ItemId { get; set; }
        public decimal DollerPrice { get; set; }
        public decimal RMBPrice { get; set; }
    }
}