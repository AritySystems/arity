//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated from a template.
//
//     Manual changes to this file may cause unexpected behavior in your application.
//     Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace AritySystems.Data
{
    using System;
    using System.Collections.Generic;
    
    public partial class SupplierCartoon
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public SupplierCartoon()
        {
            this.SupplierCartoonDetails = new HashSet<SupplierCartoonDetail>();
        }
    
        public int Id { get; set; }
        public int SupplierAssignedMapId { get; set; }
        public decimal PcsPerCartoon { get; set; }
        public int TotalCartoons { get; set; }
        public decimal NetWeight { get; set; }
        public decimal TotalNetWeight { get; set; }
        public decimal GrossWeight { get; set; }
        public decimal TotalGrossWeight { get; set; }
        public decimal CartoonBM { get; set; }
        public string CartoonNumber { get; set; }
        public int Status { get; set; }
        public System.DateTime CreatedDate { get; set; }
        public Nullable<System.DateTime> ModifiedDate { get; set; }
        public Nullable<decimal> TotalPcs { get; set; }
        public Nullable<decimal> CartoonLength { get; set; }
        public Nullable<decimal> CartoonBreadth { get; set; }
        public Nullable<decimal> CartoonHeight { get; set; }
        public string CartoonPrefix { get; set; }
        public Nullable<int> CartoonMax { get; set; }
        public Nullable<decimal> CartoonSize { get; set; }
        public Nullable<int> OrderId { get; set; }
    
        public virtual Order Order { get; set; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<SupplierCartoonDetail> SupplierCartoonDetails { get; set; }
    }
}
