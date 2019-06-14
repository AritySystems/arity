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
    
    public partial class Order
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public Order()
        {
            this.Accounts = new HashSet<Account>();
            this.CommercialInvoices = new HashSet<CommercialInvoice>();
            this.OrderLineItems = new HashSet<OrderLineItem>();
            this.Payments = new HashSet<Payment>();
            this.PerfomaInvoices = new HashSet<PerfomaInvoice>();
            this.SupplierCartoons = new HashSet<SupplierCartoon>();
        }
    
        public int Id { get; set; }
        public Nullable<int> CustomerId { get; set; }
        public string Prefix { get; set; }
        public int Status { get; set; }
        public Nullable<int> ExporterId { get; set; }
        public Nullable<System.DateTime> ModifiedDate { get; set; }
        public System.DateTime CreatedDate { get; set; }
        public int Internal_status { get; set; }
        public Nullable<int> Sales_Person_Id { get; set; }
        public Nullable<decimal> Commission { get; set; }
        public string TermsandCondition { get; set; }
        public Nullable<System.DateTime> ExpectedTimeDelivery { get; set; }
    
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<Account> Accounts { get; set; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<CommercialInvoice> CommercialInvoices { get; set; }
        public virtual Order Order1 { get; set; }
        public virtual Order Order2 { get; set; }
        public virtual User User { get; set; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<OrderLineItem> OrderLineItems { get; set; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<Payment> Payments { get; set; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<PerfomaInvoice> PerfomaInvoices { get; set; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<SupplierCartoon> SupplierCartoons { get; set; }
    }
}
