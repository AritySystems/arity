using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace AritySystems.Models
{
    public class PerformaInvoice
    {

        public string ExporterName { get; set; }
        public string ExporterAddress { get; set; }
        public string ExporterPhone { get; set; }
        public string CustomerCompanyName { get; set; }
        public string CustomerAddress { get; set; }
        public string CustomerGST { get; set; }

        public string PINo { get; set; }

        public DateTime OrderDate { get; set; }

        public string IECCode { get; set; }

        public string CustomerName { get; set; }

        public string CustomerPhone { get; set; }

        public List<PerfomaProductList> ProductList { get; set; }

        public List<CartoonDetails> CartoonList { get; set; }

    }

    public class CartoonDetails
    {
        public string SRNO { get; set; }

        public string Partiular { get; set; }

        public decimal Quantity_PCS { get; set; }

        public decimal PCSPERCartoon { get; set; }

        public int TotalCartoon { get; set; }

        public decimal NetWeight { get; set; }

        public decimal TotalNetWeight { get; set; }

        public decimal GrossWeight { get; set; }

        public decimal TotalGrossWeight { get; set; }

        public decimal? CartoonLength { get; set; }

        public decimal? CartoonBreadth { get; set; }

        public decimal? CartoonHeight { get; set; }

        public decimal CartoonCBM { get; set; }

        public string CartoonNumber { get; set; }

        public int CartoonsTotal { get; set; }

        public decimal CartoonsTotalGrossWeight { get; set; }

        public decimal CartoonsTotalNetWeight { get; set; }

        public decimal CartoonsTotalCBM { get; set; }

    }


    public class PerfomaProductList
    {
        public int SRNO { get; set; }

        public string Partiular { get; set; }

        public decimal UnitPrice { get; set; }

        public string Unit { get; set; }

        public decimal Quantity { get; set; }

        public decimal TotalUSD { get; set; }

        public int ProductId { get; set; }

        public decimal TotalRMB { get; set; }

        public decimal RMBUnitPrice { get; set; }
    }

}