using System;
using System.Collections.Generic;
using System.Linq;

namespace DeliveryOrderProcessor.Entities
{
    internal class Delivery
    {
        public Delivery()
        { }

        public Delivery(Order order)
        {
            Id = Guid.NewGuid().ToString();
            ShipToAddress = order.ShipToAddress;
            OrderItems = order.OrderItems;
            Price = order.OrderItems.Sum(i => i.UnitPrice * i.Units);
        }

        public string Id { get; set; }

        public Address ShipToAddress { get; set; }

        public List<OrderItem> OrderItems { get; set; }

        public decimal Price { get; set; }
    }
}
