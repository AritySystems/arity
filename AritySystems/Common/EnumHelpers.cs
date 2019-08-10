using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace AritySystems.Common
{
    public static class EnumHelpers
    {
        public enum Units
        {
            Nos = 1,
            Ltr = 2,
            Kg = 3,
            Gm = 4,
            Set = 5,
            Bag = 6,
            Roll = 7,
            Mtr = 8,
            ThousandNos = 9,
            tone = 10,
            Sqmtr = 11
        }

        public enum OrderStatus
        {
            Draft = 1,
            Process = 2,
            Complete = 3,
            Caceled = 4
        }

        public enum UserType
        {
            Customer = 1,
            Admin = 2,
            Sales = 3,
            Purchase = 4,
            Supplier = 5,
            Exporter = 6
        }
        public enum InternalOrderStatus
        {
            Draft = 1,
            RedyForSales = 2,
            WaitForAdminAproved = 3,
            Approved = 4,
            Rejected = 5
        }

        public enum CartoonStatus
        {
            ReadyForQC = 1,
            Dispatch = 2
        }
    }

}