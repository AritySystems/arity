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
    
    public partial class Account
    {
        public int Id { get; set; }
        public Nullable<int> CommercialId { get; set; }
        public Nullable<int> OrderId { get; set; }
        public Nullable<decimal> Dollar_Price { get; set; }
        public Nullable<decimal> RMB_Price { get; set; }
        public Nullable<System.DateTime> ModifiedDate { get; set; }
        public Nullable<System.DateTime> CreatedDate { get; set; }
        public Nullable<int> PerfomaId { get; set; }
        public Nullable<int> SupplierId { get; set; }
    
        public virtual PerfomaInvoice PerfomaInvoice { get; set; }
        public virtual User User { get; set; }
        public virtual CommercialInvoice CommercialInvoice { get; set; }
        public virtual Order Order { get; set; }
    }
}
