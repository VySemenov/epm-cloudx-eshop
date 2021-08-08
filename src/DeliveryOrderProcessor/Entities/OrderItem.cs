using System;
using System.Collections.Generic;
using System.Text;

namespace DeliveryOrderProcessor.Entities
{
    internal class OrderItem
    {
        public CatalogItemOrdered ItemOrdered { get; set; }

        public decimal UnitPrice { get; set; }

        public int Units { get; set; }
    }
}
