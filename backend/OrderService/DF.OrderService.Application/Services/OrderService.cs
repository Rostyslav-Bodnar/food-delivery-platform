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
using DF.OrderService.Contracts.Exceptions;
using DF.OrderService.Contracts.Pagination;
using DF.OrderService.Domain.Entities;


namespace DF.OrderService.Application.Services;

public class OrderService(
    IOrderRepository orderRepository,
    IEventPublisher eventPublisher,
    UserServiceRpcClient userServiceRpcClient,
    MenuServiceRpcClient menuServiceRpcClient,
    TrackingServiceRpcClient trackingServiceRpcClient,
    IOrderDishRepository orderDishRepository,
    OutboxWriter outboxWriter) : IOrderService
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
        try
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            if (request.Dishes is null || request.Dishes.Count == 0)
                throw new ArgumentException("Order must contain at least one dish");

            // Resolve every dish from MenuService in parallel so we get a server-side price snapshot.
            var distinctDishIds = request.Dishes.Select(d => d.DishId).Distinct().ToList();
            var dishLookupTasks = distinctDishIds.ToDictionary(
                id => id,
                id => menuServiceRpcClient.GetDishAsync(new GetDishRequest(id)));

            await Task.WhenAll(dishLookupTasks.Values);

            var dishInfo = new Dictionary<Guid, (decimal Price, string Name, Guid BusinessId)>();
            foreach (var (id, task) in dishLookupTasks)
            {
                var info = await task ?? throw new NotFoundException($"Dish {id} not found");
                dishInfo[id] = (info.Price, info.Name, info.BusinessId);
            }

            // Reject orders that mix businesses, or that don't match the requested business.
            if (dishInfo.Values.Any(v => v.BusinessId != request.BusinessId))
                throw new ArgumentException("All dishes must belong to the requested BusinessId");

            // Recompute the bill server-side from authoritative prices + requested quantities.
            var orderedDishes = request.Dishes.Select(d =>
            {
                var info = dishInfo[d.DishId];
                // CreateOrderDishRequest.Quantity exists in the local contract source but not yet in
                // the published DF.Contracts NuGet package — default to 1 until the package is bumped.
                var quantity = 1;
                return new OrderedDish
                {
                    OrderId = Guid.Empty,        // set after order is added to the change tracker
                    DishId = d.DishId,
                    Quantity = quantity,
                    UnitPrice = info.Price,
                    DishName = info.Name
                };
            }).ToList();

            var subtotal = orderedDishes.Sum(od => od.UnitPrice * od.Quantity);

            var order = new Order
            {
                Id = Guid.NewGuid(),
                BusinessId = request.BusinessId,
                OrderedBy = request.OrderedBy,
                OrderDate = request.OrderDate,
                TotalPrice = subtotal,           // server-authoritative; client value is ignored
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

            // Stage the outbox row; CreateWithDishesAsync's SaveChanges commits everything atomically.
            await outboxWriter.EnqueueAsync(evt);
            await orderRepository.CreateWithDishesAsync(order, orderedDishes);

            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            throw;
        }
    }

    public async Task<PagedResponse<OrderResponse>> GetAllOrdersPagedAsync(PageRequest page)
    {
        var (items, total) = await orderRepository.GetAllPagedAsync(page.Skip, page.PageSize);
        if (items.Count == 0)
            return new PagedResponse<OrderResponse>([], page.Page, page.PageSize, total);

        var businessIds = items.Select(o => o.BusinessId).Distinct().ToList();
        var businessTasks = businessIds.ToDictionary(
            id => id,
            id => SafeAwait(
                () => userServiceRpcClient.GetBusinessAccountAsync(new GetBusinessAccountRequest(id)),
                "GetBusinessAccount"));
        await Task.WhenAll(businessTasks.Values);

        var businesses = new Dictionary<Guid, string>();
        foreach (var (id, t) in businessTasks)
        {
            var b = await t;
            businesses[id] = b?.Name ?? string.Empty;
        }

        var mapped = items.Select(o => new OrderResponse(
            Id: o.Id,
            BusinessId: o.BusinessId,
            BusinessName: businesses.GetValueOrDefault(o.BusinessId, string.Empty),
            OrderedBy: o.OrderedBy,
            OrderDate: o.OrderDate,
            TotalPrice: o.TotalPrice,
            DeliveryFee: o.DeliveryFee,
            CourierFee: o.CourierFee,
            CourierPaid: o.CourierPaid)).ToList();

        return new PagedResponse<OrderResponse>(mapped, page.Page, page.PageSize, total);
    }

    public async Task<IEnumerable<OrderResponse>> GetAllOrdersAsync()
    {
        var orders = (await orderRepository.GetAll()).ToList();

        if (!orders.Any())
            return Enumerable.Empty<OrderResponse>();

        var businessTasks = orders.Select(o => o.BusinessId).Distinct().ToDictionary(
            id => id,
            id => SafeAwait(
                () => userServiceRpcClient.GetBusinessAccountAsync(new GetBusinessAccountRequest(id)),
                "GetBusinessAccount"));

        await Task.WhenAll(businessTasks.Values);

        var businesses = businessTasks.ToDictionary(x => x.Key, x => x.Value.Result);

        return orders.Select(o =>
        {
            var business = businesses.GetValueOrDefault(o.BusinessId);

            return new OrderResponse(
                Id: o.Id,
                BusinessId: o.BusinessId,
                BusinessName: business?.Name ?? string.Empty,
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
        var order = await orderRepository.GetWithDishesAsync(orderId)
                     ?? throw new NotFoundException($"Order {orderId} not found");

        var businessTask = userServiceRpcClient.GetBusinessAccountAsync(
            new GetBusinessAccountRequest(order.BusinessId));

        var customerTask = userServiceRpcClient.GetCustomerAccountAsync(
            new GetCustomerAccountRequest(order.OrderedBy));

        Task<GetCourierAccountResponse?> courierTask = order.DeliveredById != null
            ? userServiceRpcClient.GetCourierAccountAsync(
                new GetCourierAccountRequest(order.DeliveredById.Value))
            : Task.FromResult<GetCourierAccountResponse?>(null);

        await Task.WhenAll(businessTask, customerTask, courierTask);

        var business = await businessTask;
        var customer = await customerTask;
        var courier = await courierTask;

        // Order is already loaded with its OrderedDishes via Include(); no per-dish RPC needed.
        var dishDtos = order.OrderedDishes.Select(od => new DishResponse(
            Id: od.Id,
            BusinessId: order.BusinessId,
            DishName: od.DishName,
            Quantity: od.Quantity,
            Price: od.UnitPrice)).ToList();

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
        // Return every order that belongs to the business, including ones still
        // waiting for the LocationsCreatedConsumer to enrich DeliverTo/From IDs.
        // The previous .Where(... HasValue) silently hid fresh orders from the UI.
        var orders = (await orderRepository.GetOrdersByBusinessIdAsync(businessId)).ToList();

        if (!orders.Any())
            return Enumerable.Empty<BusinessOrderResponse>();

        // All downstream RPCs are best-effort: a slow/down service must not blow up
        // the whole list. Missing data renders as empty fields client-side.
        var businessTask = SafeAwait(
            () => userServiceRpcClient.GetBusinessAccountAsync(new GetBusinessAccountRequest(businessId)),
            fallback: (GetBusinessAccountResponse?)null,
            "GetBusinessAccount");

        var courierIds = orders
            .Where(o => o.DeliveredById != null)
            .Select(o => o.DeliveredById!.Value)
            .Distinct()
            .ToList();
        var courierTasks = courierIds.ToDictionary(
            id => id,
            id => SafeAwait(
                () => userServiceRpcClient.GetCourierAccountAsync(new GetCourierAccountRequest(id)),
                fallback: (GetCourierAccountResponse?)null,
                "GetCourierAccount"));

        var locationTasks = orders
            .Where(o => o.DeliverToId.HasValue && o.DeliverFromId.HasValue)
            .ToDictionary(
                o => o.Id,
                o => SafeAwait(
                    () => trackingServiceRpcClient.GetLocationsAsync(
                        new GetLocationRequest(o.DeliverToId!.Value, o.DeliverFromId!.Value)),
                    fallback: (GetLocationsResponse?)null,
                    "GetLocations"));

        await Task.WhenAll(
            new[] { (Task)businessTask }
                .Concat(courierTasks.Values.Select(t => (Task)t))
                .Concat(locationTasks.Values.Select(t => (Task)t)));

        var business = businessTask.Result;
        var couriers = courierTasks.ToDictionary(x => x.Key, x => x.Value.Result);
        var locations = locationTasks.ToDictionary(x => x.Key, x => x.Value.Result);

        var responses = new List<BusinessOrderResponse>();

        foreach (var o in orders)
        {
            var courier = o.DeliveredById != null
                ? couriers.GetValueOrDefault(o.DeliveredById.Value)
                : null;

            var location = locations.GetValueOrDefault(o.Id);

            var dishResponses = o.OrderedDishes.Select(od => new DishResponse(
                Id: od.Id,
                BusinessId: o.BusinessId,
                DishName: od.DishName,
                Quantity: od.Quantity,
                Price: od.UnitPrice)).ToList();

            responses.Add(new BusinessOrderResponse(
                Id: o.Id,
                BusinessId: o.BusinessId,
                BusinessName: business?.Name ?? string.Empty,
                OrderedBy: o.OrderedBy,
                BusinessLocation: ToLocationResponse(location?.DeliverFrom),
                CustomerLocation: ToLocationResponse(location?.DeliverTo),
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

        var businessTasks = orders.Select(o => o.BusinessId).Distinct().ToDictionary(
            id => id,
            id => SafeAwait(
                () => userServiceRpcClient.GetBusinessAccountAsync(new GetBusinessAccountRequest(id)),
                "GetBusinessAccount"));

        var courierTasks = orders
            .Where(o => o.DeliveredById != null)
            .Select(o => o.DeliveredById!.Value)
            .Distinct()
            .ToDictionary(
                id => id,
                id => SafeAwait(
                    () => userServiceRpcClient.GetCourierAccountAsync(new GetCourierAccountRequest(id)),
                    "GetCourierAccount"));

        var businessLocationTasks = orders
            .Where(o => o.DeliverFromId.HasValue)
            .ToDictionary(
                o => o.Id,
                o => GetPrimaryBusinessLocationAsync(o.BusinessId, o.DeliverFromId!.Value));

        var locationTasks = orders
            .Where(o => o.DeliverToId.HasValue && o.DeliverFromId.HasValue)
            .ToDictionary(
                o => o.Id,
                o => SafeAwait(
                    () => trackingServiceRpcClient.GetLocationsAsync(
                        new GetLocationRequest(o.DeliverToId!.Value, o.DeliverFromId!.Value)),
                    "GetLocations"));

        await Task.WhenAll(
            businessTasks.Values.Select(t => (Task)t)
                .Concat(courierTasks.Values.Select(t => (Task)t))
                .Concat(businessLocationTasks.Values.Select(t => (Task)t))
                .Concat(locationTasks.Values.Select(t => (Task)t)));

        var businesses = businessTasks.ToDictionary(x => x.Key, x => x.Value.Result);
        var couriers = courierTasks.ToDictionary(x => x.Key, x => x.Value.Result);
        var businessLocations = businessLocationTasks.ToDictionary(x => x.Key, x => x.Value.Result);
        var locations = locationTasks.ToDictionary(x => x.Key, x => x.Value.Result);

        var responses = new List<CustomerOrderResponse>();

        foreach (var order in orders)
        {
            var business = businesses.GetValueOrDefault(order.BusinessId);
            var businessLocation = businessLocations.GetValueOrDefault(order.Id);
            var location = locations.GetValueOrDefault(order.Id);
            var courier = order.DeliveredById != null
                ? couriers.GetValueOrDefault(order.DeliveredById.Value)
                : null;

            var dishResponses = order.OrderedDishes.Select(od => new DishResponse(
                Id: od.Id,
                BusinessId: order.BusinessId,
                DishName: od.DishName,
                Quantity: od.Quantity,
                Price: od.UnitPrice)).ToList();

            responses.Add(new CustomerOrderResponse(
                Id: order.Id,
                BusinessId: order.BusinessId,
                BusinessName: business?.Name ?? string.Empty,
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

        var businessTasks = orders.Select(o => o.BusinessId).Distinct().ToDictionary(
            id => id,
            id => SafeAwait(
                () => userServiceRpcClient.GetBusinessAccountAsync(new GetBusinessAccountRequest(id)),
                "GetBusinessAccount"));

        var locationTasks = orders.ToDictionary(
            o => o.Id,
            o => SafeAwait(
                () => trackingServiceRpcClient.GetLocationsAsync(
                    new GetLocationRequest(o.DeliverToId!.Value, o.DeliverFromId!.Value)),
                "GetLocations"));

        await Task.WhenAll(
            businessTasks.Values.Select(t => (Task)t)
                .Concat(locationTasks.Values.Select(t => (Task)t)));

        var businesses = businessTasks.ToDictionary(x => x.Key, x => x.Value.Result);
        var locations = locationTasks.ToDictionary(x => x.Key, x => x.Value.Result);

        return orders.Select(o =>
        {
            var business = businesses.GetValueOrDefault(o.BusinessId);
            var location = locations.GetValueOrDefault(o.Id);

            return new CourierOrderResponse(
                Id: o.Id,
                BusinessId: o.BusinessId,
                BusinessName: business?.Name ?? string.Empty,
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

        var businessTasks = orders.Select(o => o.BusinessId).Distinct().ToDictionary(
            id => id,
            id => SafeAwait(
                () => userServiceRpcClient.GetBusinessAccountAsync(new GetBusinessAccountRequest(id)),
                "GetBusinessAccount"));

        var locationTasks = orders.ToDictionary(
            o => o.Id,
            o => SafeAwait(
                () => trackingServiceRpcClient.GetLocationsAsync(
                    new GetLocationRequest(o.DeliverToId!.Value, o.DeliverFromId!.Value)),
                "GetLocations"));

        await Task.WhenAll(
            businessTasks.Values.Select(t => (Task)t)
                .Concat(locationTasks.Values.Select(t => (Task)t)));

        var businesses = businessTasks.ToDictionary(x => x.Key, x => x.Value.Result);
        var locations = locationTasks.ToDictionary(x => x.Key, x => x.Value.Result);

        return orders.Select(o =>
        {
            var business = businesses.GetValueOrDefault(o.BusinessId);
            var location = locations.GetValueOrDefault(o.Id);

            return new CourierOrderResponse(
                Id: o.Id,
                BusinessId: o.BusinessId,
                BusinessName: business?.Name ?? string.Empty,
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

    public async Task<IEnumerable<CustomerOrderResponse>> GetCustomerOrderHistoryAsync(Guid customerId)
    {
        var orders = (await orderRepository.GetOrdersByCustomerIdAsync(customerId))
            .Where(o => o.OrderStatus == OrderStatus.Canceled || o.OrderStatus == OrderStatus.Delivered)
            .ToList();

        if (!orders.Any())
            return Enumerable.Empty<CustomerOrderResponse>();

        var businessTasks = orders.Select(o => o.BusinessId).Distinct().ToDictionary(
            id => id,
            id => SafeAwait(
                () => userServiceRpcClient.GetBusinessAccountAsync(new GetBusinessAccountRequest(id)),
                "GetBusinessAccount"));

        var courierTasks = orders
            .Where(o => o.DeliveredById != null)
            .Select(o => o.DeliveredById!.Value)
            .Distinct()
            .ToDictionary(
                id => id,
                id => SafeAwait(
                    () => userServiceRpcClient.GetCourierAccountAsync(new GetCourierAccountRequest(id)),
                    "GetCourierAccount"));

        var businessLocationTasks = orders
            .Where(o => o.DeliverFromId.HasValue)
            .ToDictionary(
                o => o.Id,
                o => GetPrimaryBusinessLocationAsync(o.BusinessId, o.DeliverFromId!.Value));

        var locationTasks = orders
            .Where(o => o.DeliverToId.HasValue && o.DeliverFromId.HasValue)
            .ToDictionary(
                o => o.Id,
                o => SafeAwait(
                    () => trackingServiceRpcClient.GetLocationsAsync(
                        new GetLocationRequest(o.DeliverToId!.Value, o.DeliverFromId!.Value)),
                    "GetLocations"));

        await Task.WhenAll(
            businessTasks.Values.Select(t => (Task)t)
                .Concat(courierTasks.Values.Select(t => (Task)t))
                .Concat(businessLocationTasks.Values.Select(t => (Task)t))
                .Concat(locationTasks.Values.Select(t => (Task)t)));

        var businesses = businessTasks.ToDictionary(x => x.Key, x => x.Value.Result);
        var couriers = courierTasks.ToDictionary(x => x.Key, x => x.Value.Result);
        var businessLocations = businessLocationTasks.ToDictionary(x => x.Key, x => x.Value.Result);
        var locations = locationTasks.ToDictionary(x => x.Key, x => x.Value.Result);

        var responses = new List<CustomerOrderResponse>();

        foreach (var order in orders)
        {
            var business = businesses.GetValueOrDefault(order.BusinessId);
            var businessLocation = businessLocations.GetValueOrDefault(order.Id);
            var location = locations.GetValueOrDefault(order.Id);
            var courier = order.DeliveredById != null
                ? couriers.GetValueOrDefault(order.DeliveredById.Value)
                : null;

            var dishResponses = order.OrderedDishes.Select(od => new DishResponse(
                Id: od.Id,
                BusinessId: order.BusinessId,
                DishName: od.DishName,
                Quantity: od.Quantity,
                Price: od.UnitPrice)).ToList();

            responses.Add(new CustomerOrderResponse(
                Id: order.Id,
                BusinessId: order.BusinessId,
                BusinessName: business?.Name ?? string.Empty,
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

        var courierTask = SafeAwait(
            () => userServiceRpcClient.GetCourierAccountAsync(new GetCourierAccountRequest(courierId)),
            "GetCourierAccount");

        var businessTasks = orders.Select(o => o.BusinessId).Distinct().ToDictionary(
            id => id,
            id => SafeAwait(
                () => userServiceRpcClient.GetBusinessAccountAsync(new GetBusinessAccountRequest(id)),
                "GetBusinessAccount"));

        var locationTasks = orders
            .Where(o => o.DeliverToId.HasValue && o.DeliverFromId.HasValue)
            .ToDictionary(
                o => o.Id,
                o => SafeAwait(
                    () => trackingServiceRpcClient.GetLocationsAsync(
                        new GetLocationRequest(o.DeliverToId!.Value, o.DeliverFromId!.Value)),
                    "GetLocations"));

        await Task.WhenAll(
            new[] { (Task)courierTask }
                .Concat(businessTasks.Values.Select(t => (Task)t))
                .Concat(locationTasks.Values.Select(t => (Task)t)));

        var businesses = businessTasks.ToDictionary(x => x.Key, x => x.Value.Result);
        var locations = locationTasks.ToDictionary(x => x.Key, x => x.Value.Result);

        return orders.Select(o =>
        {
            var business = businesses.GetValueOrDefault(o.BusinessId);
            var location = locations.GetValueOrDefault(o.Id);

            return new CourierOrderResponse(
                Id: o.Id,
                BusinessId: o.BusinessId,
                BusinessName: business?.Name ?? string.Empty,
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

    public async Task<OrderResponse> ChangeOrderStatus(Guid orderId, DF.Contracts.Enums.OrderStatus status)
    {
        var order = await orderRepository.Get(orderId)
                    ?? throw new NotFoundException($"Order {orderId} not found");

        var targetStatus = ParseOrderStatus(status);
        OrderStatusTransitions.EnsureAllowed(order.OrderStatus, targetStatus);

        var shouldPublishDeliveredEvent =
            targetStatus == OrderStatus.Delivered
            && order.OrderStatus != OrderStatus.Delivered;
        var shouldPublishPickedUpEvent =
            targetStatus == OrderStatus.PickedUp
            && order.OrderStatus != OrderStatus.PickedUp;

        if ((shouldPublishDeliveredEvent || shouldPublishPickedUpEvent) && order.DeliveredById is null)
            throw new OrderStateException("Cannot move delivery forward without an assigned courier.");

        order.OrderStatus = targetStatus;

        if (order.OrderStatus == OrderStatus.Canceled)
        {
            await outboxWriter.EnqueueAsync(new OrderCancelledEvent(orderId, order.PaymentMethod.ToString()));
        }

        if (shouldPublishPickedUpEvent)
        {
            await outboxWriter.EnqueueAsync(new OrderPickedUpEvent(
                OrderId: order.Id,
                CourierId: order.DeliveredById!.Value,
                PickedUpAtUtc: DateTime.UtcNow));
        }

        if (shouldPublishDeliveredEvent
            && order.DeliveredById.HasValue
            && !order.CourierPaid
            && order.CourierFee > 0)
        {
            await outboxWriter.EnqueueAsync(new OrderDeliveredEvent(
                OrderId: order.Id,
                CourierId: order.DeliveredById.Value,
                CourierFee: order.CourierFee,
                Currency: "usd",
                DeliveredAtUtc: DateTime.UtcNow));
        }

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

    public async Task<OrderResponse> DeliverOrderAsync(Guid orderId, Guid courierId)
    {
        var order = await orderRepository.Get(orderId);

        if (order is null)
            throw new NotFoundException($"Order {orderId} not found");

        OrderStatusTransitions.EnsureAllowed(order.OrderStatus, OrderStatus.OutForDelivery);

        if (order.DeliveredById is not null && order.DeliveredById != courierId)
            throw new OrderStateException("Order is already assigned to a different courier");

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
                    ?? throw new NotFoundException($"Order {orderId} not found");

        OrderStatusTransitions.EnsureAllowed(order.OrderStatus, OrderStatus.Canceled);

        order.OrderStatus = OrderStatus.Canceled;
        await outboxWriter.EnqueueAsync(new OrderCancelledEvent(orderId, order.PaymentMethod.ToString()));
        await orderRepository.Update(order);

        return true;
    }

    private static string GenerateOrderNumber()
    {
        return $"ORD-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid().ToString()[..8]}";
    }

    /// <summary>
    /// Wraps an RPC call so a single slow/failing dependency degrades to a default value
    /// instead of failing the whole request. Used by the list endpoints, where a missing
    /// courier / location / business is preferable to a 504 for the whole page.
    /// </summary>
    private static async Task<T> SafeAwait<T>(Func<Task<T>> rpc, T fallback, string label)
    {
        try
        {
            return await rpc();
        }
        catch (Exception)
        {
            // Swallow — best-effort enrichment. The caller renders empty/null fields.
            // (Structured logger isn't injected here; the RPC client itself already logs timeouts.)
            return fallback;
        }
    }

    private static async Task<T?> SafeAwait<T>(Func<Task<T>> rpc, string label) where T : class
        => await SafeAwait(rpc, (T?)null, label);

    private async Task<GetBusinessLocationResponse?> GetPrimaryBusinessLocationAsync(Guid businessId, Guid locationId)
    {
        try
        {
            var response = await trackingServiceRpcClient.GetBusinessLocationsAsync(
                new GetBusinessLocationsRequest(businessId));

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

        return new LocationResponse(
            FullAddress: location.FullAddress,
            City: location.City,
            Street: location.Street,
            House: location.House,
            Latitude: location.Latitude.ToString(CultureInfo.InvariantCulture),
            Longitude: location.Longitude.ToString(CultureInfo.InvariantCulture)
        );
    }

    private static LocationResponse ToLocationResponse(DF.Contracts.RPC.Responses.TrackingService.LocationDTO? location)
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

    private static OrderStatus ParseOrderStatus(DF.Contracts.Enums.OrderStatus status)
    {
        var numeric = (int)status;

        if (Enum.IsDefined(typeof(OrderStatus), numeric))
        {
            return (OrderStatus)numeric;
        }

        throw new OrderStateException(
            $"Unsupported order status '{status}'.");
    }
}
