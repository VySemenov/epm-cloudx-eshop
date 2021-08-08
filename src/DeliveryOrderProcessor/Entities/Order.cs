using System;
using System.Collections.Generic;

namespace DeliveryOrderProcessor.Entities
{
    internal class Order
    {
        public string BuyerId { get; set; }

        public DateTimeOffset OrderDate { get; set; }

        public Address ShipToAddress { get; set; }

        public List<OrderItem> OrderItems { get; set; }

        public int Id { get; set; }
    }
}
