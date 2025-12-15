using DF.Contracts.EventDriven;
using DF.Contracts.RPC.Requests.UserService;
using DF.OrderService.Application.Messaging.Clients;
using DF.OrderService.Application.Messaging.Publishers;
using DF.OrderService.Application.Repositories.Interfaces;
using DF.OrderService.Application.Services.Interfaces;
using DF.OrderService.Contracts.Models.Requests;
using DF.OrderService.Contracts.Models.Responses;
using DF.OrderService.Domain.Entities;

namespace DF.OrderService.Application.Services;

public class OrderService(
    IOrderRepository orderRepository, 
    IEventPublisher eventPublisher,
    UserServiceRpcClient userServiceRpcClient) : IOrderService
{
    public async Task<bool> CreateOrdersAsync(List<CreateOrderRequest> orderRequests)
    {
        if (orderRequests == null || !orderRequests.Any())
            throw new ArgumentException("Order requests collection is empty");

        try
        {
            foreach (var request in orderRequests)
            {
                await CreateOrderAsync(request);
            }

            return true;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                "Failed to create orders batch",
                ex
            );
        }
    }

    public async Task<bool> CreateOrderAsync(CreateOrderRequest request)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        var order = new Order
        {
            Id = Guid.NewGuid(),
            BusinessId = request.BusinessId,
            OrderedBy = request.OrderedBy,
            OrderDate = request.OrderDate,
            TotalPrice = request.TotalPrice,
            OrderStatus = OrderStatus.Preparing, // або Draft/PendingLocations
            OrderNumber = GenerateOrderNumber(),
            DeliverToId = null,
            DeliverFromId = null
        };

        await orderRepository.Create(order);

        var evt = new OrderCreatedEvent(
            OrderId: order.Id,
            BusinessId: order.BusinessId,
            OrderedBy: order.OrderedBy,
            OrderDate: order.OrderDate,
            TotalPrice: order.TotalPrice,
            DeliverTo: new LocationDto(
                request.DeliverTo.FullAddress,
                request.DeliverTo.City,
                request.DeliverTo.Street,
                request.DeliverTo.House,
                request.DeliverTo.Latitude,
                request.DeliverTo.Longitude
            ),
            DeliverFrom: new LocationDto(
                request.DeliverFrom.FullAddress,
                request.DeliverFrom.City,
                request.DeliverFrom.Street,
                request.DeliverFrom.House,
                request.DeliverFrom.Latitude,
                request.DeliverFrom.Longitude
            )
        );

        await eventPublisher.PublishOrderCreatedEvent(evt);

        return true;
    }

    public async Task<IEnumerable<OrderResponse>> GetAllOrdersAsync()
    {
        var orders = (await orderRepository.GetAll()).ToList();

        if (!orders.Any())
            return Enumerable.Empty<OrderResponse>();

        var businessTasks = orders
            .Select(o => o.BusinessId)
            .Distinct()
            .ToDictionary(
                id => id,
                id => userServiceRpcClient.GetBusinessAccountAsync(
                    new GetBusinessAccountRequest(id))
            );

        await Task.WhenAll(businessTasks.Values);

        var businesses = businessTasks.ToDictionary(
            x => x.Key,
            x => x.Value.Result
        );

        return orders.Select(o =>
        {
            var business = businesses[o.BusinessId];

            return new OrderResponse(
                Id: o.Id,
                BusinessId: o.BusinessId,
                BusinessName: business.Name,
                OrderedBy: o.OrderedBy,
                OrderDate: o.OrderDate,
                TotalPrice: o.TotalPrice
            );
        });
    }
    
    public async Task<IEnumerable<BusinessOrderResponse>> GetAllByBusinessIdAsync(Guid businessId)
    {
        var orders = (await orderRepository.GetOrdersByBusinessIdAsync(businessId)).ToList();

        if (!orders.Any())
            return Enumerable.Empty<BusinessOrderResponse>();

        // 1️⃣ Business
        var businessTask = userServiceRpcClient.GetBusinessAccountAsync(
            new GetBusinessAccountRequest(businessId));

        // 2️⃣ Couriers (optional)
        var courierTasks = orders
            .Where(o => o.DeliveredBy != null)
            .Select(o => o.DeliveredBy!.Value)
            .Distinct()
            .ToDictionary(
                id => id,
                id => userServiceRpcClient.GetCourierAccountAsync(
                    new GetCourierAccountRequest(id))
            );

        // 3️⃣ Customers (OrderedBy)
        var customerTasks = orders
            .Select(o => o.OrderedBy)
            .Distinct()
            .ToDictionary(
                id => id,
                id => userServiceRpcClient.GetCustomerAccountAsync(
                    new GetCustomerAccountRequest(id))
            );

        // 4️⃣ Await ALL
        var allTasks = courierTasks.Values
            .Select(t => (Task)t)
            .Concat(customerTasks.Values.Select(t => (Task)t))
            .Append(businessTask);

        await Task.WhenAll(allTasks);

        // 5️⃣ Collect results
        var business = await businessTask;

        var couriers = courierTasks.ToDictionary(
            x => x.Key,
            x => x.Value.Result
        );

        var customers = customerTasks.ToDictionary(
            x => x.Key,
            x => x.Value.Result
        );

        // 6️⃣ Map
        return orders.Select(o =>
        {
            var courier = o.DeliveredBy != null
                ? couriers.GetValueOrDefault(o.DeliveredBy.Value)
                : null;

            var customer = customers[o.OrderedBy];

            return new BusinessOrderResponse(
                Id: o.Id,
                BusinessId: o.BusinessId,
                BusinessName: business.Name,
                OrderedBy: o.OrderedBy,
                CustomerFullName: $"{customer.Name} {customer.Surname}",
                CustomerAddress: customer.Address,
                OrderDate: o.OrderDate,
                TotalPrice: o.TotalPrice,
                DeliveredBy: o.DeliveredBy ?? Guid.Empty,
                CourierName: courier != null
                    ? $"{courier.Name} {courier.Surname}"
                    : string.Empty,
                OrderStatus: o.OrderStatus.ToString()
            );
        });
    }

    
    public async Task<IEnumerable<CustomerOrderResponse>> GetAllByCustomerIdAsync(Guid customerId)
{
    var orders = (await orderRepository.GetOrdersByCustomerIdAsync(customerId)).ToList();

    if (!orders.Any())
        return Enumerable.Empty<CustomerOrderResponse>();

    // 1️⃣ Customer (один раз)
    var customerTask = userServiceRpcClient.GetCustomerAccountAsync(
        new GetCustomerAccountRequest(customerId)
    );

    // 2️⃣ Бізнеси (fan-out паралельно)
    var businessTasks = orders
        .Select(o => o.BusinessId)
        .Distinct()
        .ToDictionary(
            id => id,
            id => userServiceRpcClient.GetBusinessAccountAsync(
                new GetBusinessAccountRequest(id)
            )
        );

    // 3️⃣ Курʼєри (fan-out паралельно)
    var courierTasks = orders
        .Where(o => o.DeliveredBy != null)
        .Select(o => o.DeliveredBy!.Value)
        .Distinct()
        .ToDictionary(
            id => id,
            id => userServiceRpcClient.GetCourierAccountAsync(
                new GetCourierAccountRequest(id)
            )
        );

    await Task.WhenAll(
        businessTasks.Values
            .Concat<Task>(courierTasks.Values)
            .Append(customerTask)
    );

    var customer = await customerTask;

    // 4️⃣ Dictionaries для швидкого доступу
    var businesses = businessTasks.ToDictionary(
        x => x.Key,
        x => x.Value.Result
    );

    var couriers = courierTasks.ToDictionary(
        x => x.Key,
        x => x.Value.Result
    );

    // 5️⃣ DTO generation
    return orders.Select(order =>
    {
        var business = businesses[order.BusinessId];
        var courier = order.DeliveredBy != null
            ? couriers.GetValueOrDefault(order.DeliveredBy.Value)
            : null;

        return new CustomerOrderResponse(
            Id: order.Id,
            BusinessId: order.BusinessId,
            BusinessName: business.Name,
            BusinessAddress: business.Adresses.FirstOrDefault() ?? string.Empty,
            OrderedBy: order.OrderedBy,
            OrderDate: order.OrderDate,
            TotalPrice: order.TotalPrice,
            DeliveredBy: order.DeliveredBy ?? Guid.Empty,
            CourierName: courier != null
                ? $"{courier.Name} {courier.Surname}"
                : string.Empty,
            OrderStatus: order.OrderStatus.ToString()
        );
    });
}


    public async Task<IEnumerable<CourierOrderResponse>> GetAllByCourierIdAsync(Guid courierId)
    {
        var orders = (await orderRepository.GetOrdersByCourierIdAsync(courierId)).ToList();

        if (!orders.Any())
            return Enumerable.Empty<CourierOrderResponse>();

        // 1️⃣ Courier (сам курʼєр)
        var courierTask = userServiceRpcClient.GetCourierAccountAsync(
            new GetCourierAccountRequest(courierId));

        // 2️⃣ Businesses
        var businessTasks = orders
            .Select(o => o.BusinessId)
            .Distinct()
            .ToDictionary(
                id => id,
                id => userServiceRpcClient.GetBusinessAccountAsync(
                    new GetBusinessAccountRequest(id))
            );

        // 3️⃣ Customers (OrderedBy)
        var customerTasks = orders
            .Select(o => o.OrderedBy)
            .Distinct()
            .ToDictionary(
                id => id,
                id => userServiceRpcClient.GetCustomerAccountAsync(
                    new GetCustomerAccountRequest(id))
            );

        // 4️⃣ Await ALL
        var allTasks = businessTasks.Values
            .Select(t => (Task)t)
            .Concat(customerTasks.Values.Select(t => (Task)t))
            .Append(courierTask);

        await Task.WhenAll(allTasks);

        // 5️⃣ Collect results
        var courier = await courierTask;

        var businesses = businessTasks.ToDictionary(
            x => x.Key,
            x => x.Value.Result
        );

        var customers = customerTasks.ToDictionary(
            x => x.Key,
            x => x.Value.Result
        );

        // 6️⃣ Map
        return orders.Select(o =>
        {
            var business = businesses[o.BusinessId];
            var customer = customers[o.OrderedBy];

            return new CourierOrderResponse(
                Id: o.Id,
                BusinessId: o.BusinessId,
                BusinessName: business.Name,
                OrderedBy: o.OrderedBy,
                CustomerFullName: $"{customer.Name} {customer.Surname}",
                CustomerAddress: customer.Address,
                OrderDate: o.OrderDate,
                TotalPrice: o.TotalPrice,
                OrderStatus: o.OrderStatus.ToString()
            );
        });
    }



    public async Task<OrderResponse> ChangeOrderStatus(Guid orderId, OrderStatus status)
    {
        var order = await orderRepository.Get(orderId)
                    ?? throw new InvalidOperationException($"Order {orderId} not found");

        order.OrderStatus = status;
        await orderRepository.Update(order);

        var business = await userServiceRpcClient.GetBusinessAccountAsync(
            new GetBusinessAccountRequest(order.BusinessId));

        return new OrderResponse(
            Id: order.Id,
            BusinessId: order.BusinessId,
            BusinessName: business.Name,
            OrderedBy: order.OrderedBy,
            OrderDate: order.OrderDate,
            TotalPrice: order.TotalPrice
        );
    }

    
    private static string GenerateOrderNumber()
    {
        return $"ORD-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid().ToString()[..8]}";
    }
}