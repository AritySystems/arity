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
    
    public partial class BOM_Mapper
    {
        public int Id { get; set; }
        public Nullable<int> ProductId { get; set; }
        public Nullable<int> ChildId { get; set; }
        public Nullable<decimal> BOM { get; set; }
    
        public virtual Product Product { get; set; }
        public virtual Product Product1 { get; set; }
    }
}
