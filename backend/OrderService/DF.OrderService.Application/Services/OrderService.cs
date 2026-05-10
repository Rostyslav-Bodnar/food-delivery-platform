using System.Globalization;
using DF.Contracts.EventDriven;
using DF.Contracts.Gateway.Requests.Order;
using DF.Contracts.Gateway.Responses.Order;
using DF.Contracts.RPC.Requests.MenuService;
using DF.Contracts.RPC.Requests.TrackingService;
using DF.Contracts.RPC.Requests.UserService;
using DF.Contracts.RPC.Responses.TrackingService;
using DF.Contracts.RPC.Responses.UserService;
using DF.OrderService.Application.Mappers;
using DF.OrderService.Application.Messaging.Clients;
using DF.OrderService.Application.Messaging.Publishers;
using DF.OrderService.Application.Repositories.Interfaces;
using DF.OrderService.Application.Services.Interfaces;
using DF.OrderService.Domain.Entities;

namespace DF.OrderService.Application.Services;

public class OrderService(
    IOrderRepository orderRepository,
    IEventPublisher eventPublisher,
    UserServiceRpcClient userServiceRpcClient,
    MenuServiceRpcClient menuServiceRpcClient,
    TrackingServiceRpcClient trackingServiceRpcClient,
    IOrderDishRepository orderDishRepository) : IOrderService
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
            OrderStatus = OrderStatus.Preparing,
            OrderNumber = GenerateOrderNumber(),
            DeliverToId = null,
            DeliverFromId = null,
            DeliveryFee = 0,
            CourierFee = 0,
            CourierPaid = false,
            Profit = 0,
            PaymentMethod = request.PaymentMethod.ToDomain()
        };

        var orderEntity = await orderRepository.Create(order);

        foreach (var dish in request.Dishes)
        {
            await orderDishRepository.Create(new OrderedDish
            {
                DishId = dish.DishId,
                OrderId = orderEntity.Id
            });
        }

        var account = await userServiceRpcClient.GetBusinessAccountAsync(
            new GetBusinessAccountRequest(order.BusinessId));

        var evt = new OrderCreatedEvent(
            OrderId: order.Id,
            BusinessId: order.BusinessId,
            OrderedBy: order.OrderedBy,
            OrderDate: order.OrderDate,
            TotalPrice: order.TotalPrice,
            DeliverTo: new LocationDto(request.DeliverTo.FullAddress),
            DeliverFrom: new LocationDto(request.DeliverFrom.FullAddress),
            Currency: "usd",
            PaymentMethod: order.PaymentMethod.ToString(),
            BusinessStripeAccountId: account.StripeId
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
                id => userServiceRpcClient.GetBusinessAccountAsync(new GetBusinessAccountRequest(id))
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
                TotalPrice: o.TotalPrice,
                DeliveryFee: o.DeliveryFee,
                CourierFee: o.CourierFee,
                CourierPaid: o.CourierPaid
            );
        });
    }

    public async Task<OrderDetailsResponse> GetOrderAsync(Guid orderId)
    {
        var order = await orderRepository.Get(orderId)
                     ?? throw new InvalidOperationException($"Order {orderId} not found");

        var businessTask = userServiceRpcClient.GetBusinessAccountAsync(
            new GetBusinessAccountRequest(order.BusinessId));

        var customerTask = userServiceRpcClient.GetCustomerAccountAsync(
            new GetCustomerAccountRequest(order.OrderedBy));

        Task<GetCourierAccountResponse?> courierTask = order.DeliveredById != null
            ? userServiceRpcClient.GetCourierAccountAsync(
                new GetCourierAccountRequest(order.DeliveredById.Value))
            : Task.FromResult<GetCourierAccountResponse?>(null);

        var dishesTask = menuServiceRpcClient.GetDishesAsync(new GetDishesRequest(order.Id));

        await Task.WhenAll(businessTask, customerTask, courierTask, dishesTask);

        var business = await businessTask;
        var customer = await customerTask;
        var courier = await courierTask;
        var dishesResponse = await dishesTask;

        var dishDtos = dishesResponse.Dishes.Select(d =>
            new DishResponse(
                Id: d.DishId,
                BusinessId: d.BusinessId,
                DishName: d.Name,
                Quantity: 1,
                Price: d.Price
            )
        ).ToList();

        return new OrderDetailsResponse(
            Id: order.Id,
            BusinessId: order.BusinessId,
            BusinessName: business.Name,
            OrderedById: order.OrderedBy,
            CustomerFullName: $"{customer.Name} {customer.Surname}",
            CustomerAddress: customer.Address,
            CustomerPhoneNumber: customer.PhoneNumber,
            OrderDate: order.OrderDate,
            TotalPrice: order.TotalPrice,
            DeliveryFee: order.DeliveryFee,
            CourierFee: order.CourierFee,
            CourierPaid: order.CourierPaid,
            OrderStatus: order.OrderStatus.ToString(),
            Profit: order.Profit,
            dishes: dishDtos,
            DeliveredById: order.DeliveredById,
            CourierName: courier != null ? $"{courier.Name} {courier.Surname}" : null,
            CourierPhoneNumber: courier?.PhoneNumber
        );
    }

    public async Task<IEnumerable<BusinessOrderResponse>> GetAllByBusinessIdAsync(Guid businessId)
    {
        var orders = (await orderRepository.GetOrdersByBusinessIdAsync(businessId))
            .Where(o => o.DeliverToId.HasValue && o.DeliverFromId.HasValue)
            .ToList();

        if (!orders.Any())
            return Enumerable.Empty<BusinessOrderResponse>();

        var businessTask = userServiceRpcClient.GetBusinessAccountAsync(
            new GetBusinessAccountRequest(businessId));

        var courierTasks = orders
            .Where(o => o.DeliveredById != null)
            .Select(o => o.DeliveredById!.Value)
            .Distinct()
            .ToDictionary(
                id => id,
                id => userServiceRpcClient.GetCourierAccountAsync(new GetCourierAccountRequest(id))
            );

        var locationTasks = orders.ToDictionary(
            o => o.Id,
            o => trackingServiceRpcClient.GetLocationsAsync(
                new GetLocationRequest(o.DeliverToId!.Value, o.DeliverFromId!.Value))
        );

        var allTasks = courierTasks.Values
            .Select(t => (Task)t)
            .Concat(locationTasks.Values.Select(t => (Task)t))
            .Append(businessTask);

        await Task.WhenAll(allTasks);

        var business = await businessTask;
        var couriers = courierTasks.ToDictionary(x => x.Key, x => x.Value.Result);
        var locations = locationTasks.ToDictionary(x => x.Key, x => x.Value.Result);

        var responses = new List<BusinessOrderResponse>();

        foreach (var o in orders)
        {
            var courier = o.DeliveredById != null
                ? couriers.GetValueOrDefault(o.DeliveredById.Value)
                : null;

            var location = locations[o.Id];
            var orderedDishes = await orderDishRepository.GetOrderDishesByOrderId(o.Id);

            var dishResponses = new List<DishResponse>();

            foreach (var od in orderedDishes)
            {
                var dishInfo = await menuServiceRpcClient.GetDishAsync(new GetDishRequest(od.DishId));

                dishResponses.Add(new DishResponse(
                    Id: od.Id,
                    BusinessId: dishInfo.BusinessId,
                    DishName: dishInfo.Name,
                    Quantity: 1,
                    Price: dishInfo.Price
                ));
            }

            responses.Add(new BusinessOrderResponse(
                Id: o.Id,
                BusinessId: o.BusinessId,
                BusinessName: business.Name,
                OrderedBy: o.OrderedBy,
                BusinessLocation: ToLocationResponse(location.DeliverFrom),
                CustomerLocation: ToLocationResponse(location.DeliverTo),
                CourierLocation: EmptyLocation(),
                OrderDate: o.OrderDate,
                TotalPrice: o.TotalPrice,
                DeliveryFee: o.DeliveryFee,
                CourierFee: o.CourierFee,
                CourierPaid: o.CourierPaid,
                DeliveredBy: o.DeliveredById ?? Guid.Empty,
                CourierName: courier != null ? $"{courier.Name} {courier.Surname}" : string.Empty,
                OrderStatus: o.OrderStatus.ToString(),
                dishes: dishResponses
            ));
        }

        return responses;
    }

    public async Task<IEnumerable<CustomerOrderResponse>> GetAllByCustomerIdAsync(Guid customerId)
    {
        var orders = (await orderRepository.GetOrdersByCustomerIdAsync(customerId))
            .Where(o => o.OrderStatus != OrderStatus.Canceled && o.OrderStatus != OrderStatus.Delivered)
            .ToList();

        if (!orders.Any())
            return Enumerable.Empty<CustomerOrderResponse>();

        var businessTasks = orders
            .Select(o => o.BusinessId)
            .Distinct()
            .ToDictionary(
                id => id,
                id => userServiceRpcClient.GetBusinessAccountAsync(new GetBusinessAccountRequest(id))
            );

        var courierTasks = orders
            .Where(o => o.DeliveredById != null)
            .Select(o => o.DeliveredById!.Value)
            .Distinct()
            .ToDictionary(
                id => id,
                id => userServiceRpcClient.GetCourierAccountAsync(new GetCourierAccountRequest(id))
            );

        var businessLocationTasks = orders
            .Where(o => o.DeliverFromId.HasValue)
            .ToDictionary(
                o => o.Id,
                o => GetPrimaryBusinessLocationAsync(
                    o.BusinessId,
                    o.DeliverFromId!.Value
                )
            );

        var locationTasks = orders
            .Where(o => o.DeliverToId.HasValue && o.DeliverFromId.HasValue)
            .ToDictionary(
                o => o.Id,
                o => trackingServiceRpcClient.GetLocationsAsync(
                    new GetLocationRequest(o.DeliverToId!.Value, o.DeliverFromId!.Value))
            );

        var allTasks = businessTasks.Values
            .Select(t => (Task)t)
            .Concat(courierTasks.Values.Select(t => (Task)t))
            .Concat(businessLocationTasks.Values.Select(t => (Task)t))
            .Concat(locationTasks.Values.Select(t => (Task)t));

        await Task.WhenAll(allTasks);

        var businesses = businessTasks.ToDictionary(x => x.Key, x => x.Value.Result);
        var couriers = courierTasks.ToDictionary(x => x.Key, x => x.Value.Result);
        var businessLocations = businessLocationTasks.ToDictionary(x => x.Key, x => x.Value.Result);
        var locations = locationTasks.ToDictionary(x => x.Key, x => x.Value.Result);

        var responses = new List<CustomerOrderResponse>();

        foreach (var order in orders)
        {
            var business = businesses[order.BusinessId];
            var businessLocation = businessLocations.GetValueOrDefault(order.Id);
            var location = locations.GetValueOrDefault(order.Id);
            var courier = order.DeliveredById != null
                ? couriers.GetValueOrDefault(order.DeliveredById.Value)
                : null;

            var orderedDishes = await orderDishRepository.GetOrderDishesByOrderId(order.Id);

            var dishResponses = new List<DishResponse>();
            foreach (var od in orderedDishes)
            {
                var dishInfo = await menuServiceRpcClient.GetDishAsync(new GetDishRequest(od.DishId));

                dishResponses.Add(new DishResponse(
                    Id: od.Id,
                    BusinessId: dishInfo.BusinessId,
                    DishName: dishInfo.Name,
                    Quantity: 1,
                    Price: dishInfo.Price
                ));
            }

            responses.Add(new CustomerOrderResponse(
                Id: order.Id,
                BusinessId: order.BusinessId,
                BusinessName: business.Name,
                BusinessLocation: ToLocationResponse(businessLocation),
                CustomerLocation: ToLocationResponse(location?.DeliverTo),
                CourierLocation: EmptyLocation(),
                OrderedBy: order.OrderedBy,
                OrderDate: order.OrderDate,
                TotalPrice: order.TotalPrice,
                DeliveryFee: order.DeliveryFee,
                CourierFee: order.CourierFee,
                CourierPaid: order.CourierPaid,
                DeliveredBy: order.DeliveredById ?? Guid.Empty,
                CourierName: courier != null ? $"{courier.Name} {courier.Surname}" : string.Empty,
                OrderStatus: order.OrderStatus.ToString(),
                dishes: dishResponses
            ));
        }

        return responses;
    }

    public async Task<IEnumerable<CourierOrderResponse>> GetAllByCourierIdAsync(Guid courierId)
    {
        var orders = (await orderRepository.GetAll())
            .Where(o =>
                o.OrderStatus == OrderStatus.Ready
                && o.DeliveredById == null
                && o.DeliverToId.HasValue
                && o.DeliverFromId.HasValue)
            .ToList();

        if (!orders.Any())
            return Enumerable.Empty<CourierOrderResponse>();

        var businessTasks = orders
            .Select(o => o.BusinessId)
            .Distinct()
            .ToDictionary(
                id => id,
                id => userServiceRpcClient.GetBusinessAccountAsync(new GetBusinessAccountRequest(id))
            );

        var locationTasks = orders.ToDictionary(
            o => o.Id,
            o => trackingServiceRpcClient.GetLocationsAsync(
                new GetLocationRequest(o.DeliverToId!.Value, o.DeliverFromId!.Value))
        );

        var allTasks = businessTasks.Values
            .Select(t => (Task)t)
            .Concat(locationTasks.Values.Select(t => (Task)t));

        await Task.WhenAll(allTasks);

        var businesses = businessTasks.ToDictionary(x => x.Key, x => x.Value.Result);
        var locations = locationTasks.ToDictionary(x => x.Key, x => x.Value.Result);

        return orders.Select(o =>
        {
            var business = businesses[o.BusinessId];
            var location = locations[o.Id];

            return new CourierOrderResponse(
                Id: o.Id,
                BusinessId: o.BusinessId,
                BusinessName: business.Name,
                OrderedBy: o.OrderedBy,
                BusinessLocation: ToLocationResponse(location.DeliverFrom),
                CustomerLocation: ToLocationResponse(location.DeliverTo),
                CourierLocation: EmptyLocation(),
                OrderDate: o.OrderDate,
                TotalPrice: o.TotalPrice,
                DeliveryFee: o.DeliveryFee,
                CourierFee: o.CourierFee,
                CourierPaid: o.CourierPaid,
                OrderStatus: o.OrderStatus.ToString(),
                Profit: o.Profit
            );
        });
    }

    public async Task<IEnumerable<CourierOrderResponse>> GetActiveByCourierIdAsync(Guid courierId)
    {
        var orders = (await orderRepository.GetOrdersByCourierIdAsync(courierId))
            .Where(o =>
                o.OrderStatus != OrderStatus.Canceled
                && o.OrderStatus != OrderStatus.Delivered
                && o.DeliverToId.HasValue
                && o.DeliverFromId.HasValue)
            .ToList();

        if (!orders.Any())
            return Enumerable.Empty<CourierOrderResponse>();

        var businessTasks = orders
            .Select(o => o.BusinessId)
            .Distinct()
            .ToDictionary(
                id => id,
                id => userServiceRpcClient.GetBusinessAccountAsync(new GetBusinessAccountRequest(id))
            );

        var locationTasks = orders.ToDictionary(
            o => o.Id,
            o => trackingServiceRpcClient.GetLocationsAsync(
                new GetLocationRequest(o.DeliverToId!.Value, o.DeliverFromId!.Value))
        );

        var allTasks = businessTasks.Values
            .Select(t => (Task)t)
            .Concat(locationTasks.Values.Select(t => (Task)t));

        await Task.WhenAll(allTasks);

        var businesses = businessTasks.ToDictionary(x => x.Key, x => x.Value.Result);
        var locations = locationTasks.ToDictionary(x => x.Key, x => x.Value.Result);

        return orders.Select(o =>
        {
            var business = businesses[o.BusinessId];
            var location = locations[o.Id];

            return new CourierOrderResponse(
                Id: o.Id,
                BusinessId: o.BusinessId,
                BusinessName: business.Name,
                OrderedBy: o.OrderedBy,
                BusinessLocation: ToLocationResponse(location.DeliverFrom),
                CustomerLocation: ToLocationResponse(location.DeliverTo),
                CourierLocation: EmptyLocation(),
                OrderDate: o.OrderDate,
                TotalPrice: o.TotalPrice,
                DeliveryFee: o.DeliveryFee,
                CourierFee: o.CourierFee,
                CourierPaid: o.CourierPaid,
                OrderStatus: o.OrderStatus.ToString(),
                Profit: o.Profit
            );
        });
    }

    public async Task<IEnumerable<CustomerOrderResponse>> GetCustomerOrderHistoryAsync(Guid customerId)
    {
        var orders = (await orderRepository.GetOrdersByCustomerIdAsync(customerId))
            .Where(o => o.OrderStatus == OrderStatus.Canceled || o.OrderStatus == OrderStatus.Delivered)
            .ToList();

        if (!orders.Any())
            return Enumerable.Empty<CustomerOrderResponse>();

        var businessTasks = orders
            .Select(o => o.BusinessId)
            .Distinct()
            .ToDictionary(
                id => id,
                id => userServiceRpcClient.GetBusinessAccountAsync(new GetBusinessAccountRequest(id))
            );

        var courierTasks = orders
            .Where(o => o.DeliveredById != null)
            .Select(o => o.DeliveredById!.Value)
            .Distinct()
            .ToDictionary(
                id => id,
                id => userServiceRpcClient.GetCourierAccountAsync(new GetCourierAccountRequest(id))
            );
        
        var businessLocationTasks = orders
            .Where(o => o.DeliverFromId.HasValue)
            .ToDictionary(
                o => o.Id,
                o => GetPrimaryBusinessLocationAsync(
                    o.BusinessId,
                    o.DeliverFromId!.Value
                )
            );

        var locationTasks = orders
            .Where(o => o.DeliverToId.HasValue && o.DeliverFromId.HasValue)
            .ToDictionary(
                o => o.Id,
                o => trackingServiceRpcClient.GetLocationsAsync(
                    new GetLocationRequest(o.DeliverToId!.Value, o.DeliverFromId!.Value))
            );

        var allTasks = businessTasks.Values
            .Select(t => (Task)t)
            .Concat(courierTasks.Values.Select(t => (Task)t))
            .Concat(businessLocationTasks.Values.Select(t => (Task)t))
            .Concat(locationTasks.Values.Select(t => (Task)t));

        await Task.WhenAll(allTasks);

        var businesses = businessTasks.ToDictionary(x => x.Key, x => x.Value.Result);
        var couriers = courierTasks.ToDictionary(x => x.Key, x => x.Value.Result);
        var businessLocations = businessLocationTasks.ToDictionary(x => x.Key, x => x.Value.Result);
        var locations = locationTasks.ToDictionary(x => x.Key, x => x.Value.Result);

        var responses = new List<CustomerOrderResponse>();

        foreach (var order in orders)
        {
            var business = businesses[order.BusinessId];
            var businessLocation = businessLocations.GetValueOrDefault(order.Id);
            var location = locations.GetValueOrDefault(order.Id);
            var courier = order.DeliveredById != null
                ? couriers.GetValueOrDefault(order.DeliveredById.Value)
                : null;

            var orderedDishes = await orderDishRepository.GetOrderDishesByOrderId(order.Id);

            var dishResponses = new List<DishResponse>();
            foreach (var od in orderedDishes)
            {
                var dishInfo = await menuServiceRpcClient.GetDishAsync(new GetDishRequest(od.DishId));

                dishResponses.Add(new DishResponse(
                    Id: od.Id,
                    BusinessId: dishInfo.BusinessId,
                    DishName: dishInfo.Name,
                    Quantity: 1,
                    Price: dishInfo.Price
                ));
            }

            responses.Add(new CustomerOrderResponse(
                Id: order.Id,
                BusinessId: order.BusinessId,
                BusinessName: business.Name,
                BusinessLocation: ToLocationResponse(businessLocation),
                CustomerLocation: ToLocationResponse(location?.DeliverTo),
                CourierLocation: EmptyLocation(),
                OrderedBy: order.OrderedBy,
                OrderDate: order.OrderDate,
                TotalPrice: order.TotalPrice,
                DeliveryFee: order.DeliveryFee,
                CourierFee: order.CourierFee,
                CourierPaid: order.CourierPaid,
                DeliveredBy: order.DeliveredById ?? Guid.Empty,
                CourierName: courier != null ? $"{courier.Name} {courier.Surname}" : string.Empty,
                OrderStatus: order.OrderStatus.ToString(),
                dishes: dishResponses
            ));
        }

        return responses;
    }

    public async Task<IEnumerable<CourierOrderResponse>> GetCourierOrderHistoryAsync(Guid courierId)
    {
        var orders = (await orderRepository.GetOrdersByCourierIdAsync(courierId))
            .Where(o => o.OrderStatus == OrderStatus.Canceled || o.OrderStatus == OrderStatus.Delivered)
            .ToList();

        if (!orders.Any())
            return Enumerable.Empty<CourierOrderResponse>();

        var courierTask = userServiceRpcClient.GetCourierAccountAsync(new GetCourierAccountRequest(courierId));

        var businessTasks = orders
            .Select(o => o.BusinessId)
            .Distinct()
            .ToDictionary(
                id => id,
                id => userServiceRpcClient.GetBusinessAccountAsync(new GetBusinessAccountRequest(id))
            );

        var locationTasks = orders
            .Where(o => o.DeliverToId.HasValue && o.DeliverFromId.HasValue)
            .ToDictionary(
                o => o.Id,
                o => trackingServiceRpcClient.GetLocationsAsync(
                    new GetLocationRequest(o.DeliverToId!.Value, o.DeliverFromId!.Value))
            );

        var allTasks = businessTasks.Values.Select(t => (Task)t)
            .Concat(locationTasks.Values.Select(t => (Task)t))
            .Append(courierTask);

        await Task.WhenAll(allTasks);

        var businesses = businessTasks.ToDictionary(x => x.Key, x => x.Value.Result);
        var locations = locationTasks.ToDictionary(x => x.Key, x => x.Value.Result);

        return orders.Select(o =>
        {
            var business = businesses[o.BusinessId];
            var location = locations.GetValueOrDefault(o.Id);

            return new CourierOrderResponse(
                Id: o.Id,
                BusinessId: o.BusinessId,
                BusinessName: business.Name,
                OrderedBy: o.OrderedBy,
                BusinessLocation: ToLocationResponse(location?.DeliverFrom),
                CustomerLocation: ToLocationResponse(location?.DeliverTo),
                CourierLocation: EmptyLocation(),
                OrderDate: o.OrderDate,
                TotalPrice: o.TotalPrice,
                DeliveryFee: o.DeliveryFee,
                CourierFee: o.CourierFee,
                CourierPaid: o.CourierPaid,
                OrderStatus: o.OrderStatus.ToString(),
                Profit: o.Profit
            );
        });
    }

    public async Task<OrderResponse> ChangeOrderStatus(Guid orderId, OrderStatus status)
    {
        var order = await orderRepository.Get(orderId)
                    ?? throw new InvalidOperationException($"Order {orderId} not found");

        var shouldPublishDeliveredEvent =
            status == OrderStatus.Delivered
            && order.OrderStatus != OrderStatus.Delivered;

        if (shouldPublishDeliveredEvent && order.DeliveredById is null)
            throw new InvalidOperationException("Cannot mark order as delivered without assigned courier.");

        order.OrderStatus = status;
        await orderRepository.Update(order);
        var business = await userServiceRpcClient.GetBusinessAccountAsync(
            new GetBusinessAccountRequest(order.BusinessId));

        if (order.OrderStatus == OrderStatus.Canceled)
        {
            await eventPublisher.PublishOrderCanceledEvent(
                new OrderCancelledEvent(orderId, order.PaymentMethod.ToString()));
        }

        if (shouldPublishDeliveredEvent
            && order.DeliveredById.HasValue
            && !order.CourierPaid
            && order.CourierFee > 0)
        {
            await eventPublisher.PublishOrderDeliveredEvent(
                new OrderDeliveredEvent(
                    OrderId: order.Id,
                    CourierId: order.DeliveredById.Value,
                    CourierFee: order.CourierFee,
                    Currency: "usd",
                    DeliveredAtUtc: DateTime.UtcNow));
        }

        return new OrderResponse(
            Id: order.Id,
            BusinessId: order.BusinessId,
            BusinessName: business.Name,
            OrderedBy: order.OrderedBy,
            OrderDate: order.OrderDate,
            TotalPrice: order.TotalPrice,
            DeliveryFee: order.DeliveryFee,
            CourierFee: order.CourierFee,
            CourierPaid: order.CourierPaid
        );
    }

    public async Task<OrderResponse> DeliverOrderAsync(Guid orderId, Guid courierId)
    {
        var order = await orderRepository.Get(orderId);

        if (order is null)
            throw new InvalidOperationException($"Order {orderId} not found");

        order.DeliveredById = courierId;
        order.OrderStatus = OrderStatus.OutForDelivery;

        await orderRepository.Update(order);

        var business = await userServiceRpcClient.GetBusinessAccountAsync(
            new GetBusinessAccountRequest(order.BusinessId));

        return new OrderResponse(
            Id: order.Id,
            BusinessId: order.BusinessId,
            BusinessName: business.Name,
            OrderedBy: order.OrderedBy,
            OrderDate: order.OrderDate,
            TotalPrice: order.TotalPrice,
            DeliveryFee: order.DeliveryFee,
            CourierFee: order.CourierFee,
            CourierPaid: order.CourierPaid
        );
    }

    public async Task<bool> CancelOrderAsync(Guid orderId)
    {
        var order = await orderRepository.Get(orderId)
                    ?? throw new InvalidOperationException($"Order {orderId} not found");

        order.OrderStatus = OrderStatus.Canceled;
        await orderRepository.Update(order);
        await eventPublisher.PublishOrderCanceledEvent(
            new OrderCancelledEvent(orderId, order.PaymentMethod.ToString()));

        return true;
    }

    private static string GenerateOrderNumber()
    {
        return $"ORD-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid().ToString()[..8]}";
    }

    private async Task<GetBusinessLocationResponse?> GetPrimaryBusinessLocationAsync(Guid businessId, Guid locationId)
    {
        try
        {
            var response = await trackingServiceRpcClient.GetBusinessLocationsAsync(
                new GetBusinessLocationsRequest(businessId));

            if (response.BusinessLocations.Any(bl => bl.LocationId == new Guid("019d5e76-5d41-7e9f-addd-dcdb4ae63862")))
            {
                
            }
            
            return response.BusinessLocations.FirstOrDefault(bl => bl.LocationId == locationId);
        }
        catch (TimeoutException)
        {
            return null;
        }
    }

    private static LocationResponse ToLocationResponse(GetBusinessLocationResponse? location)
    {
        if (location is null)
            return EmptyLocation();
        
        if(location.LocationId == new Guid("019d5e76-5d41-7e9f-addd-dcdb4ae63862"))
        {
            
        }
        
        return new LocationResponse(
            FullAddress: location.FullAddress,
            City: location.City,
            Street: location.Street,
            House: location.House,
            Latitude: location.Latitude.ToString(CultureInfo.InvariantCulture),
            Longitude: location.Longitude.ToString(CultureInfo.InvariantCulture)
        );
    }

    private static LocationResponse ToLocationResponse(dynamic? location)
    {
        if (location is null)
            return EmptyLocation();

        return new LocationResponse(
            FullAddress: location.FullAddress ?? string.Empty,
            City: location.City ?? string.Empty,
            Street: location.Street ?? string.Empty,
            House: location.House ?? string.Empty,
            Latitude: location.Latitude.ToString(CultureInfo.InvariantCulture),
            Longitude: location.Longitude.ToString(CultureInfo.InvariantCulture)
        );
    }

    private static LocationResponse EmptyLocation()
    {
        return new LocationResponse(
            FullAddress: string.Empty,
            City: string.Empty,
            Street: string.Empty,
            House: string.Empty,
            Latitude: string.Empty,
            Longitude: string.Empty
        );
    }
}
