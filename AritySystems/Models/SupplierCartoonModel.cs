using System;
using System.ComponentModel.DataAnnotations;

namespace AritySystems.Models
{
    public class SupplierCartoonModel
    {
        public int Id { get; set; }


        public int SupplierAssignedMapId { get; set; }

        [Required]
       // [RegularExpression(@"^[0-9](\.[0-9]+)?$", ErrorMessage = "Valid Decimal number with maximum 2 decimal places.")]
        public decimal PcsPerCartoon { get; set; }

        [Required]
        //[RegularExpression(@"^[0-9](\.[0-9]+)?$", ErrorMessage = "Valid Decimal number with maximum 2 decimal places.")]
        public int TotalCartoons { get; set; }

        [Required]
        //[RegularExpression(@"^[0-9](\.[0-9]+)?$", ErrorMessage = "Valid Decimal number with maximum 2 decimal places.")]
        public decimal NetWeight { get; set; }

        
        public decimal TotalNetWeight { get; set; }

        [Required]
        //[RegularExpression(@"^[0-9](\.[0-9]+)?$", ErrorMessage = "Valid Decimal number with maximum 2 decimal places.")]
        public decimal GrossWeight { get; set; }

        public decimal TotalGrossWeight { get; set; }

        [Required]
       // [RegularExpression(@"^[0-9](\.[0-9]+)?$", ErrorMessage = "Valid Decimal number with maximum 2 decimal places.")]
        public decimal CartoonSize { get; set; }

        [Required]
        //[RegularExpression(@"^[0-9](\.[0-9]+)?$", ErrorMessage = "Valid Decimal number with maximum 2 decimal places.")]
        public decimal CartoonBM { get; set; }

        public string CartoonNumber { get; set; }
        public int Status { get; set; }
        public System.DateTime CreatedDate { get; set; }
        public Nullable<System.DateTime> ModifiedDate { get; set; }

        public int OrderId { get; set; }

        public int SupplierOrderMapId { get; set; }

    }
}