using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace AritySystems.Models
{
    public class ExpectedDeliveryDateModel
    {
        public DateTime? ExpectedDeliveryDate { get; set; }
        public int SupplierId { get; set; }
        public int OrderId { get; set; }
    }
}