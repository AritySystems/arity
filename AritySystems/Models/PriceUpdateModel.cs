using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace AritySystems.Models
{
    public class PriceUpdateModel
    {
        public int ItemId { get; set; }
        public int DollerPrice { get; set; }
        public int RMBPrice { get; set; }
    }
}