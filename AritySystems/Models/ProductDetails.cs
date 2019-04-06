using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace AritySystems.Models
{
    public class ProductDetails
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Product Name - Chinese")]
        public string Chinese_Name { get; set; }

        [Required]
        [Display(Name = "Product Name - English")]
        public string English_Name { get; set; }

        [Required]
        [Display(Name = "MOQ")]
        public decimal Quantity { get; set; }

        [Required]
        [Display(Name = "Dollar Price")]
        public decimal Dollar_Price { get; set; }

        [Required]
        [Display(Name = "RMB Price")]
        public decimal RMB_Price { get; set; }

        [Required]
        [Display(Name = "Unit")]
        public string Unit { get; set; }

        public string Description { get; set; }

        public DateTime? ModifiedDate { get; set; }

        public bool IsActive { get; set; }
        public string Suppliers { get; set; }

        [Display(Name = "Parent Products")]
        public string ParentIds { get; set; }

        public int[] ParentIdsArray { get; set; }
        public int[] SuppliersArray { get; set; }

        public int Parent_Id { get; set; }

        public System.DateTime CreatedDate { get; set; }

        [Display(Name = "Pack Size")]
        public decimal BOM { get; set; }

        [Display(Name = "Cubic Meter")]
        public decimal Cubic_Meter { get; set; }

        [Display(Name = "Weight")]
        public decimal Weight { get; set; }

    }
}