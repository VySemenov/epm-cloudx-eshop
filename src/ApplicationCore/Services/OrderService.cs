using Ardalis.GuardClauses;
using BlazorShared;
using Microsoft.Azure.ServiceBus;
using Microsoft.eShopWeb.ApplicationCore.Entities;
using Microsoft.eShopWeb.ApplicationCore.Entities.BasketAggregate;
using Microsoft.eShopWeb.ApplicationCore.Entities.OrderAggregate;
using Microsoft.eShopWeb.ApplicationCore.Interfaces;
using Microsoft.eShopWeb.ApplicationCore.Specifications;
using Microsoft.Extensions.Configuration;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace Microsoft.eShopWeb.ApplicationCore.Services
{
    public class OrderService : IOrderService
    {
        private readonly IAsyncRepository<Order> _orderRepository;
        private readonly IUriComposer _uriComposer;
        private readonly IAsyncRepository<Basket> _basketRepository;
        private readonly IAsyncRepository<CatalogItem> _itemRepository;
        private readonly string _ordersItemsReserverFunctionUri;
        private readonly string _deliveryOrderProcessorUri;
        private readonly IConfiguration _configuration;

        public OrderService(IAsyncRepository<Basket> basketRepository,
            IAsyncRepository<CatalogItem> itemRepository,
            IAsyncRepository<Order> orderRepository,
            IUriComposer uriComposer,
            BaseUrlConfiguration baseUrlConfiguration, IConfiguration configuration)
        {
            _orderRepository = orderRepository;
            _uriComposer = uriComposer;
            _configuration = configuration;
            _basketRepository = basketRepository;
            _itemRepository = itemRepository;
            _ordersItemsReserverFunctionUri = baseUrlConfiguration.OrderItemsReserver;
            _deliveryOrderProcessorUri = baseUrlConfiguration.DeliveryOrderProcessor;
        }

        public async Task CreateOrderAsync(int basketId, Address shippingAddress)
        {
            var basketSpec = new BasketWithItemsSpecification(basketId);
            var basket = await _basketRepository.FirstOrDefaultAsync(basketSpec);

            Guard.Against.NullBasket(basketId, basket);
            Guard.Against.EmptyBasketOnCheckout(basket.Items);

            var catalogItemsSpecification = new CatalogItemsSpecification(basket.Items.Select(item => item.CatalogItemId).ToArray());
            var catalogItems = await _itemRepository.ListAsync(catalogItemsSpecification);

            var items = basket.Items.Select(basketItem =>
            {
                var catalogItem = catalogItems.First(c => c.Id == basketItem.CatalogItemId);
                var itemOrdered = new CatalogItemOrdered(catalogItem.Id, catalogItem.Name, _uriComposer.ComposePicUri(catalogItem.PictureUri));
                var orderItem = new OrderItem(itemOrdered, basketItem.UnitPrice, basketItem.Quantity);
                return orderItem;
            }).ToList();

            var order = new Order(basket.BuyerId, shippingAddress, items);

            await _orderRepository.AddAsync(order);

            /*using (var httpClient = new HttpClient())
            {
                var content = JsonContent.Create(order, typeof(Order));
                await httpClient.PostAsync(_ordersItemsReserverFunctionUri, content);
            }*/

            using (var httpClient = new HttpClient())
            {
                var content = JsonContent.Create(order, typeof(Order));
                await httpClient.PostAsync(_deliveryOrderProcessorUri, content);
            }

            var connectionStringBuilder = new ServiceBusConnectionStringBuilder(_configuration.GetConnectionString("ServiceBusConnection"));
            var queueClient = new QueueClient(connectionStringBuilder);
            try
            {
                await queueClient.SendAsync(new Message(JsonContent.Create(order, typeof(Order)).ReadAsByteArrayAsync().Result));
            }
            finally
            {
                await queueClient.CloseAsync();
            }
        }
    }
}
