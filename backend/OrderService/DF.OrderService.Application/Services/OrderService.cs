using System.Collections.Concurrent;
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
using LocationDTO = DF.Contracts.RPC.Responses.TrackingService.LocationDTO;

namespace DF.OrderService.Application.Services;

public class OrderService(
    IOrderRepository orderRepository,
    UserServiceRpcClient userServiceRpcClient,
    MenuServiceRpcClient menuServiceRpcClient,
    TrackingServiceRpcClient trackingServiceRpcClient,
    OutboxWriter outboxWriter) : IOrderService
{
    private const int BatchSize = 50;

    // =========================
    // CREATE METHODS
    // =========================

    public async Task<bool> CreateOrderAsync(CreateOrderRequest request)
    {
        ValidateCreateOrderRequest(request);

        var dishes = await LoadDishInfoAsync(request);

        var orderedDishes = request.Dishes.Select(d =>
        {
            var dish = dishes[d.DishId];

            return new OrderedDish
            {
                OrderId = Guid.Empty,
                DishId = d.DishId,
                Quantity = d.Quantity > 0 ? d.Quantity : 1,
                UnitPrice = dish.Price,
                DishName = dish.Name
            };
        }).ToList();

        var order = BuildOrder(request, orderedDishes);

        var business = await SafeAwait(
            () => userServiceRpcClient.GetBusinessAccountAsync(
                new GetBusinessAccountRequest(order.BusinessId)),
            fallback: (GetBusinessAccountResponse?)null);

        await outboxWriter.EnqueueAsync(new OrderCreatedEvent(
            order.Id,
            order.BusinessId,
            order.OrderedBy,
            order.OrderDate,
            order.TotalPrice,
            new LocationDto(request.DeliverTo.FullAddress),
            new LocationDto(request.DeliverFrom.FullAddress),
            "usd",
            order.PaymentMethod.ToString(),
            business?.StripeId ?? string.Empty));

        await orderRepository.CreateWithDishesAsync(order, orderedDishes);

        return true;
    }

    public async Task<bool> CreateOrdersAsync(List<CreateOrderRequest> orderRequests)
    {
        if (orderRequests is null || orderRequests.Count == 0)
            throw new ArgumentException("Empty batch");

        foreach (var request in orderRequests)
        {
            ValidateCreateOrderRequest(request);
        }

        // =========================
        // LOAD ALL DISHES IN BATCH
        // =========================

        var allDishIds = orderRequests
            .SelectMany(x => x.Dishes)
            .Select(x => x.DishId)
            .Distinct()
            .ToList();

        var dishesResponse = await menuServiceRpcClient.GetDishesBatchAsync(
            new GetDishesBatchRequest(allDishIds));

        var dishes = dishesResponse.Dishes.ToDictionary(
            x => x.DishId,
            x => x);

        // =========================
        // VALIDATE BUSINESS MIX
        // =========================

        foreach (var request in orderRequests)
        {
            foreach (var orderedDish in request.Dishes)
            {
                if (!dishes.TryGetValue(orderedDish.DishId, out var dish))
                {
                    throw new NotFoundException(
                        $"Dish {orderedDish.DishId} not found");
                }

                if (dish.BusinessId != request.BusinessId)
                {
                    throw new ArgumentException(
                        $"Dish {orderedDish.DishId} does not belong to business {request.BusinessId}");
                }
            }
        }

        // =========================
        // LOAD BUSINESSES IN BATCH
        // =========================

        var businessIds = orderRequests
            .Select(x => x.BusinessId)
            .Distinct()
            .ToList();

        var businesses = await SafeAwait(
            () => userServiceRpcClient.GetBusinessAccountsBatchAsync(
                new GetBusinessAccountsBatchRequest(businessIds)),
            fallback: new List<GetBusinessAccountResponse>());

        var businessesMap = businesses.ToDictionary(
            x => x.AccountId,
            x => x);

        // =========================
        // BUILD ORDERS
        // =========================

        var orders = new List<Order>();
        var orderedDishes = new List<OrderedDish>();

        foreach (var request in orderRequests)
        {
            var currentOrderedDishes = request.Dishes.Select(d =>
            {
                var dish = dishes[d.DishId];

                return new OrderedDish
                {
                    OrderId = Guid.Empty,
                    DishId = d.DishId,
                    Quantity = d.Quantity > 0 ? d.Quantity : 1,
                    UnitPrice = dish.Price,
                    DishName = dish.Name
                };
            }).ToList();

            var order = BuildOrder(request, currentOrderedDishes);

            foreach (var orderedDish in currentOrderedDishes)
            {
                orderedDish.OrderId = order.Id;
            }

            orders.Add(order);
            orderedDishes.AddRange(currentOrderedDishes);

            businessesMap.TryGetValue(order.BusinessId, out var business);

            await outboxWriter.EnqueueAsync(new OrderCreatedEvent(
                order.Id,
                order.BusinessId,
                order.OrderedBy,
                order.OrderDate,
                order.TotalPrice,
                new LocationDto(request.DeliverTo.FullAddress),
                new LocationDto(request.DeliverFrom.FullAddress),
                "usd",
                order.PaymentMethod.ToString(),
                business?.StripeId ?? string.Empty));
        }

        // =========================
        // SAVE IN SINGLE DB CALL
        // =========================

        await orderRepository.CreateRangeWithDishesAsync(
            orders,
            orderedDishes);

        return true;
    }

    // =========================
    // GET METHODS
    // =========================

    public async Task<PagedResponse<OrderResponse>> GetAllOrdersPagedAsync(PageRequest page)
    {
        var (orders, total) = await orderRepository.GetAllPagedAsync(page.Skip, page.PageSize);

        if (orders.Count == 0)
            return new PagedResponse<OrderResponse>([], page.Page, page.PageSize, total);

        var businesses = await LoadBusinessesAsync(
            orders.Select(x => x.BusinessId));

        var responses = orders.Select(order =>
        {
            businesses.TryGetValue(order.BusinessId, out var business);

            return MapToOrderResponse(order, business);
        }).ToList();

        return new PagedResponse<OrderResponse>(
            responses,
            page.Page,
            page.PageSize,
            total);
    }

    public async Task<IEnumerable<OrderResponse>> GetAllOrdersAsync()
    {
        var orders = (await orderRepository.GetAll()).ToList();

        if (orders.Count == 0)
            return Enumerable.Empty<OrderResponse>();

        var businesses = await LoadBusinessesAsync(
            orders.Select(x => x.BusinessId));

        return orders.Select(order =>
        {
            businesses.TryGetValue(order.BusinessId, out var business);

            return MapToOrderResponse(order, business);
        });
    }

    public async Task<OrderDetailsResponse> GetOrderAsync(Guid orderId)
    {
        var order = await orderRepository.GetWithDishesAsync(orderId)
                    ?? throw new NotFoundException($"Order {orderId} not found");

        var businessTask = SafeAwait(
            () => userServiceRpcClient.GetBusinessAccountAsync(
                new GetBusinessAccountRequest(order.BusinessId)),
            fallback: (GetBusinessAccountResponse?)null);

        var customerTask = SafeAwait(
            () => userServiceRpcClient.GetCustomerAccountAsync(
                new GetCustomerAccountRequest(order.OrderedBy)),
            fallback: (GetCustomerAccountResponse?)null);

        var courierTask = order.DeliveredById.HasValue
            ? SafeAwait(
                () => userServiceRpcClient.GetCourierAccountAsync(
                    new GetCourierAccountRequest(order.DeliveredById.Value)),
                fallback: (GetCourierAccountResponse?)null)
            : Task.FromResult<GetCourierAccountResponse?>(null);

        await Task.WhenAll(businessTask, customerTask, courierTask);

        var business = await businessTask;
        var customer = await customerTask;
        var courier = await courierTask;

        return new OrderDetailsResponse(
            Id: order.Id,
            BusinessId: order.BusinessId,
            BusinessName: business?.Name ?? string.Empty,
            OrderedById: order.OrderedBy,
            CustomerFullName: customer is null
                ? string.Empty
                : $"{customer.Name} {customer.Surname}",
            CustomerAddress: customer?.Address ?? string.Empty,
            CustomerPhoneNumber: customer?.PhoneNumber ?? string.Empty,
            OrderDate: order.OrderDate,
            TotalPrice: order.TotalPrice,
            DeliveryFee: order.DeliveryFee,
            CourierFee: order.CourierFee,
            CourierPaid: order.CourierPaid,
            OrderStatus: order.OrderStatus.ToString(),
            Profit: order.Profit,
            dishes: MapDishResponses(order),
            DeliveredById: order.DeliveredById,
            CourierName: courier is null
                ? null
                : $"{courier.Name} {courier.Surname}",
            CourierPhoneNumber: courier?.PhoneNumber,
            DeliveryMethod: order.DeliveryMethod.ToString());
    }

    public async Task<IEnumerable<BusinessOrderResponse>> GetAllByBusinessIdAsync(Guid businessId)
    {
        var orders = (await orderRepository.GetOrdersByBusinessIdAsync(businessId))
            .ToList();

        if (orders.Count == 0)
            return Enumerable.Empty<BusinessOrderResponse>();

        var businessTask = SafeAwait(
            () => userServiceRpcClient.GetBusinessAccountAsync(
                new GetBusinessAccountRequest(businessId)),
            fallback: (GetBusinessAccountResponse?)null);

        var couriersTask = LoadCouriersAsync(
            orders.Where(x => x.DeliveredById.HasValue)
                  .Select(x => x.DeliveredById!.Value));

        var locationsTask = LoadLocationsAsync(orders);

        await Task.WhenAll(
            businessTask,
            couriersTask,
            locationsTask);

        var business = await businessTask;
        var couriers = await couriersTask;
        var locations = await locationsTask;

        return orders.Select(order =>
        {
            couriers.TryGetValue(order.DeliveredById ?? Guid.Empty, out var courier);
            locations.TryGetValue(order.Id, out var location);

            return new BusinessOrderResponse(
                Id: order.Id,
                BusinessId: order.BusinessId,
                BusinessName: business?.Name ?? string.Empty,
                OrderedBy: order.OrderedBy,
                BusinessLocation: ToLocationResponse(location?.DeliverFrom),
                CustomerLocation: ToLocationResponse(location?.DeliverTo),
                CourierLocation: EmptyLocation(),
                OrderDate: order.OrderDate,
                TotalPrice: order.TotalPrice,
                DeliveryFee: order.DeliveryFee,
                CourierFee: order.CourierFee,
                CourierPaid: order.CourierPaid,
                DeliveredBy: order.DeliveredById ?? Guid.Empty,
                CourierName: courier is null
                    ? string.Empty
                    : $"{courier.Name} {courier.Surname}",
                OrderStatus: order.OrderStatus.ToString(),
                dishes: MapDishResponses(order),
                DeliveryMethod: order.DeliveryMethod.ToString());
        });
    }

    public async Task<IEnumerable<CustomerOrderResponse>> GetAllByCustomerIdAsync(Guid customerId)
    {
        var orders = (await orderRepository.GetOrdersByCustomerIdAsync(customerId))
            .Where(x => x.OrderStatus != OrderStatus.Canceled
                     && x.OrderStatus != OrderStatus.Delivered)
            .ToList();

        var result = await BuildCustomerOrdersAsync(orders);
        return result;
    }

    public async Task<IEnumerable<CustomerOrderResponse>> GetCustomerOrderHistoryAsync(Guid customerId)
    {
        var orders = (await orderRepository.GetOrdersByCustomerIdAsync(customerId))
            .Where(x => x.OrderStatus == OrderStatus.Canceled
                     || x.OrderStatus == OrderStatus.Delivered)
            .ToList();

        return await BuildCustomerOrdersAsync(orders);
    }

    public async Task<IEnumerable<CourierOrderResponse>> GetAllByCourierIdAsync(Guid courierId)
    {
        var orders = (await orderRepository.GetAll())
            .Where(x => x.OrderStatus == OrderStatus.Ready
                     && x.DeliveredById == null
                     && x.DeliverToId.HasValue
                     && x.DeliverFromId.HasValue
                     && x.DeliveryMethod != DeliveryMethod.Pickup)
            .ToList();

        return await BuildCourierOrdersAsync(orders);
    }

    public async Task<IEnumerable<CourierOrderResponse>> GetActiveByCourierIdAsync(Guid courierId)
    {
        var orders = (await orderRepository.GetOrdersByCourierIdAsync(courierId))
            .Where(x => x.OrderStatus != OrderStatus.Canceled
                     && x.OrderStatus != OrderStatus.Delivered
                     && x.DeliverToId.HasValue
                     && x.DeliverFromId.HasValue)
            .ToList();

        return await BuildCourierOrdersAsync(orders);
    }

    public async Task<IEnumerable<CourierOrderResponse>> GetCourierOrderHistoryAsync(Guid courierId)
    {
        var orders = (await orderRepository.GetOrdersByCourierIdAsync(courierId))
            .Where(x => x.OrderStatus == OrderStatus.Canceled
                     || x.OrderStatus == OrderStatus.Delivered)
            .ToList();

        return await BuildCourierOrdersAsync(orders);
    }

    // =========================
    // STATUS METHODS
    // =========================

    public async Task<OrderResponse> ChangeOrderStatus(
        Guid orderId,
        DF.Contracts.Enums.OrderStatus status)
    {
        var order = await orderRepository.Get(orderId)
                    ?? throw new NotFoundException($"Order {orderId} not found");

        var targetStatus = ParseOrderStatus(status);

        OrderStatusTransitions.EnsureAllowed(order, targetStatus);

        ValidateCourierAssignment(order, targetStatus);

        var publishPickedUp =
            targetStatus == OrderStatus.PickedUp
            && order.OrderStatus != OrderStatus.PickedUp;

        var publishDelivered =
            targetStatus == OrderStatus.Delivered
            && order.OrderStatus != OrderStatus.Delivered;

        order.OrderStatus = targetStatus;

        await PublishStatusEventsAsync(
            order,
            publishPickedUp,
            publishDelivered);

        await orderRepository.Update(order);

        var business = await SafeAwait(
            () => userServiceRpcClient.GetBusinessAccountAsync(
                new GetBusinessAccountRequest(order.BusinessId)),
            fallback: (GetBusinessAccountResponse?)null);

        return MapToOrderResponse(order, business);
    }

    public async Task<OrderResponse> DeliverOrderAsync(
        Guid orderId,
        Guid courierId)
    {
        var order = await orderRepository.Get(orderId)
                    ?? throw new NotFoundException($"Order {orderId} not found");

        if (order.DeliveryMethod == DeliveryMethod.Pickup)
        {
            throw new OrderStateException(
                "Pickup orders cannot be claimed by couriers; customer collects at the restaurant.");
        }

        OrderStatusTransitions.EnsureAllowed(order, OrderStatus.OutForDelivery);

        if (order.DeliveredById is not null
            && order.DeliveredById != courierId)
        {
            throw new OrderStateException(
                "Order is already assigned to another courier");
        }

        order.DeliveredById = courierId;
        order.OrderStatus = OrderStatus.OutForDelivery;

        await orderRepository.Update(order);

        var business = await SafeAwait(
            () => userServiceRpcClient.GetBusinessAccountAsync(
                new GetBusinessAccountRequest(order.BusinessId)),
            fallback: (GetBusinessAccountResponse?)null);

        return MapToOrderResponse(order, business);
    }

    public async Task<bool> CancelOrderAsync(Guid orderId)
    {
        var order = await orderRepository.Get(orderId)
                    ?? throw new NotFoundException($"Order {orderId} not found");

        OrderStatusTransitions.EnsureAllowed(order, OrderStatus.Canceled);

        order.OrderStatus = OrderStatus.Canceled;

        await outboxWriter.EnqueueAsync(
            new OrderCancelledEvent(
                order.Id,
                order.PaymentMethod.ToString()));

        await orderRepository.Update(order);

        return true;
    }

    // =========================
    // PRIVATE BUILDERS
    // =========================

    private async Task<IEnumerable<CustomerOrderResponse>> BuildCustomerOrdersAsync(
        List<Order> orders)
    {
        if (orders.Count == 0)
            return Enumerable.Empty<CustomerOrderResponse>();

        var businessesTask = LoadBusinessesAsync(
            orders.Select(x => x.BusinessId));

        var couriersTask = LoadCouriersAsync(
            orders.Where(x => x.DeliveredById.HasValue)
                  .Select(x => x.DeliveredById!.Value));

        var businessLocationsTask = LoadBusinessLocationsAsync(orders);

        var locationsTask = LoadLocationsAsync(orders);

        await Task.WhenAll(
            businessesTask,
            couriersTask,
            businessLocationsTask,
            locationsTask);

        var businesses = await businessesTask;
        var couriers = await couriersTask;
        var businessLocations = await businessLocationsTask;
        var locations = await locationsTask;

        return orders.Select(order =>
        {
            businesses.TryGetValue(order.BusinessId, out var business);

            couriers.TryGetValue(
                order.DeliveredById ?? Guid.Empty,
                out var courier);

            businessLocations.TryGetValue(order.Id, out var businessLocation);

            locations.TryGetValue(order.Id, out var location);

            return new CustomerOrderResponse(
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
                CourierName: courier is null
                    ? string.Empty
                    : $"{courier.Name} {courier.Surname}",
                OrderStatus: order.OrderStatus.ToString(),
                dishes: MapDishResponses(order),
                DeliveryMethod: order.DeliveryMethod.ToString());
        });
    }

    private async Task<IEnumerable<CourierOrderResponse>> BuildCourierOrdersAsync(
        List<Order> orders)
    {
        if (orders.Count == 0)
            return Enumerable.Empty<CourierOrderResponse>();

        var businessesTask = LoadBusinessesAsync(
            orders.Select(x => x.BusinessId));

        var locationsTask = LoadLocationsAsync(orders);

        await Task.WhenAll(
            businessesTask,
            locationsTask);

        var businesses = await businessesTask;
        var locations = await locationsTask;

        return orders.Select(order =>
        {
            businesses.TryGetValue(order.BusinessId, out var business);
            locations.TryGetValue(order.Id, out var location);

            return new CourierOrderResponse(
                Id: order.Id,
                BusinessId: order.BusinessId,
                BusinessName: business?.Name ?? string.Empty,
                OrderedBy: order.OrderedBy,
                BusinessLocation: ToLocationResponse(location?.DeliverFrom),
                CustomerLocation: ToLocationResponse(location?.DeliverTo),
                CourierLocation: EmptyLocation(),
                OrderDate: order.OrderDate,
                TotalPrice: order.TotalPrice,
                DeliveryFee: order.DeliveryFee,
                CourierFee: order.CourierFee,
                CourierPaid: order.CourierPaid,
                OrderStatus: order.OrderStatus.ToString(),
                Profit: order.Profit);
        });
    }

    // =========================
    // PRIVATE LOADERS
    // =========================

    private async Task<Dictionary<Guid, GetBusinessAccountResponse?>>
        LoadBusinessesAsync(IEnumerable<Guid> ids)
    {
        var distinctIds = ids
            .Distinct()
            .ToList();

        if (distinctIds.Count == 0)
            return [];

        var result = new ConcurrentDictionary<Guid, GetBusinessAccountResponse?>();

        var batches = distinctIds.Chunk(BatchSize);

        var tasks = batches.Select(async batch =>
        {
            var response = await SafeAwait(
                () => userServiceRpcClient.GetBusinessAccountsBatchAsync(
                    new GetBusinessAccountsBatchRequest(batch.ToList())),
                fallback: []);

            foreach (var business in response)
            {
                result.TryAdd(business.AccountId, business);
            }
        });

        await Task.WhenAll(tasks);

        return result.ToDictionary();
    }

    private async Task<Dictionary<Guid, GetCourierAccountResponse?>>
        LoadCouriersAsync(IEnumerable<Guid> ids)
    {
        var distinctIds = ids
            .Distinct()
            .ToList();

        if (distinctIds.Count == 0)
            return [];

        var result = new ConcurrentDictionary<Guid, GetCourierAccountResponse?>();

        var batches = distinctIds.Chunk(BatchSize);

        var tasks = batches.Select(async batch =>
        {
            var response = await SafeAwait(
                () => userServiceRpcClient.GetCourierAccountsBatchAsync(
                    new GetCourierAccountsBatchRequest(batch.ToList())),
                fallback: []);

            foreach (var courier in response)
            {
                result.TryAdd(courier.AccountId, courier);
            }
        });

        await Task.WhenAll(tasks);

        return result.ToDictionary();
    }

    private async Task<Dictionary<Guid, GetLocationsResponse?>>
        LoadLocationsAsync(IEnumerable<Order> orders)
    {
        var validOrders = orders
            .Where(x => x.DeliverToId.HasValue && x.DeliverFromId.HasValue)
            .ToList();

        if (validOrders.Count == 0)
            return [];
        
        var batchRequest = new GetLocationsBatchRequest(
            validOrders.Select(o =>
                    new GetLocationRequest(
                        o.DeliverToId!.Value,
                        o.DeliverFromId!.Value))
                .ToList());
        
        var responses = await SafeAwait(
            () => trackingServiceRpcClient.GetLocationsBatchAsync(batchRequest),
            fallback: new List<GetLocationsResponse>());

        var responseList = responses.ToList();
        
        var result = new Dictionary<Guid, GetLocationsResponse?>();

        for (int i = 0; i < validOrders.Count; i++)
        {
            var order = validOrders[i];

            var response = i < responseList.Count
                ? responseList[i]
                : null;

            result[order.Id] = response;
        }

        return result;
    }

    private async Task<Dictionary<Guid, GetBusinessLocationResponse?>> LoadBusinessLocationsAsync(IEnumerable<Order> orders)
    {
        var validOrders = orders
            .Where(x => x.DeliverFromId.HasValue)
            .ToList();

        if (validOrders.Count == 0)
            return [];

        var businessIds = validOrders
            .Select(x => x.BusinessId)
            .Distinct()
            .ToList();

        var locationsByBusiness =
            new Dictionary<Guid, List<GetBusinessLocationResponse>>();

        var batches = businessIds.Chunk(BatchSize);

        var tasks = batches.Select(async batch =>
        {
            var response = await SafeAwait(
                () => trackingServiceRpcClient.GetBusinessLocationsBatchAsync(
                    new GetBusinessLocationsBatchRequest(batch.ToList())),
                fallback: new List<GetBusinessLocationsResponse>());

            foreach (var businessLocations in response)
            {
                locationsByBusiness[businessLocations.BusinessId] =
                    businessLocations.BusinessLocations.ToList();
            }
        });

        await Task.WhenAll(tasks);

        var result = new Dictionary<Guid, GetBusinessLocationResponse?>();

        foreach (var order in validOrders)
        {
            locationsByBusiness.TryGetValue(
                order.BusinessId,
                out var businessLocations);

            var location = businessLocations?
                .FirstOrDefault(x =>
                    x.LocationId == order.DeliverFromId);

            result[order.Id] = location;
        }

        return result;
    }

    private async Task<Dictionary<Guid, (decimal Price, string Name, Guid BusinessId)>> LoadDishInfoAsync(CreateOrderRequest request)
    {
        var dishIds = request.Dishes
            .Select(x => x.DishId)
            .Distinct()
            .ToList();

        if (dishIds.Count == 0)
            throw new ArgumentException("No dishes provided");

        var response = await menuServiceRpcClient.GetDishesBatchAsync(
            new GetDishesBatchRequest(dishIds));

        var dishes = response?.Dishes?.ToList()
                     ?? throw new NotFoundException("Dishes not found");

        // ✅ перевірка що всі dishes існують
        var foundIds = dishes.Select(d => d.DishId).ToHashSet();

        var missing = dishIds.Where(id => !foundIds.Contains(id)).ToList();

        if (missing.Count > 0)
            throw new NotFoundException($"Dishes not found: {string.Join(", ", missing)}");

        // ✅ перевірка бізнесу
        if (dishes.Any(x => x.BusinessId != request.BusinessId))
            throw new ArgumentException("Invalid business mix");

        return dishes.ToDictionary(
            x => x.DishId,
            x => (x.Price, x.Name, x.BusinessId));
    }

    // =========================
    // PRIVATE HELPERS
    // =========================

    private static void ValidateCreateOrderRequest(
        CreateOrderRequest request)
    {
        if (request is null)
            throw new ArgumentNullException(nameof(request));

        if (request.Dishes is null
            || request.Dishes.Count == 0)
        {
            throw new ArgumentException(
                "Order must contain at least one dish");
        }
    }

    private static Order BuildOrder(
        CreateOrderRequest request,
        List<OrderedDish> dishes)
    {
        return new Order
        {
            Id = Guid.NewGuid(),
            BusinessId = request.BusinessId,
            OrderedBy = request.OrderedBy,
            OrderDate = request.OrderDate,
            TotalPrice = dishes.Sum(x => x.UnitPrice * x.Quantity),
            OrderStatus = OrderStatus.Preparing,
            OrderNumber = GenerateOrderNumber(),
            PaymentMethod = request.PaymentMethod.ToDomain(),
            DeliveryMethod = request.DeliveryMethod.ToDomain()
        };
    }

    private async Task PublishStatusEventsAsync(
        Order order,
        bool publishPickedUp,
        bool publishDelivered)
    {
        if (order.OrderStatus == OrderStatus.Canceled)
        {
            await outboxWriter.EnqueueAsync(
                new OrderCancelledEvent(
                    order.Id,
                    order.PaymentMethod.ToString()));
        }

        if (publishPickedUp)
        {
            await outboxWriter.EnqueueAsync(
                new OrderPickedUpEvent(
                    OrderId: order.Id,
                    CourierId: order.DeliveredById!.Value,
                    PickedUpAtUtc: DateTime.UtcNow));
        }

        if (publishDelivered
            && order.DeliveredById.HasValue
            && !order.CourierPaid
            && order.CourierFee > 0)
        {
            await outboxWriter.EnqueueAsync(
                new OrderDeliveredEvent(
                    OrderId: order.Id,
                    CourierId: order.DeliveredById.Value,
                    CourierFee: order.CourierFee,
                    Currency: "usd",
                    DeliveredAtUtc: DateTime.UtcNow));
        }
    }

    private static void ValidateCourierAssignment(
        Order order,
        OrderStatus targetStatus)
    {
        // Pickup orders have no courier; the business marks them Delivered directly.
        if (order.DeliveryMethod == DeliveryMethod.Pickup)
            return;

        var requiresCourier =
            targetStatus == OrderStatus.PickedUp
            || targetStatus == OrderStatus.Delivered;

        if (requiresCourier
            && order.DeliveredById is null)
        {
            throw new OrderStateException(
                "Cannot move delivery forward without an assigned courier.");
        }
    }

    private static OrderResponse MapToOrderResponse(
        Order order,
        GetBusinessAccountResponse? business)
    {
        return new OrderResponse(
            Id: order.Id,
            BusinessId: order.BusinessId,
            BusinessName: business?.Name ?? string.Empty,
            OrderedBy: order.OrderedBy,
            OrderDate: order.OrderDate,
            TotalPrice: order.TotalPrice,
            DeliveryFee: order.DeliveryFee,
            CourierFee: order.CourierFee,
            CourierPaid: order.CourierPaid);
    }

    private static List<DishResponse> MapDishResponses(Order order)
    {
        return order.OrderedDishes.Select(dish =>
            new DishResponse(
                Id: dish.Id,
                BusinessId: order.BusinessId,
                DishName: dish.DishName,
                Quantity: dish.Quantity,
                Price: dish.UnitPrice))
            .ToList();
    }

    private static string GenerateOrderNumber()
    {
        return
            $"ORD-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid().ToString()[..8]}";
    }

    private static async Task<T> SafeAwait<T>(
        Func<Task<T>> rpc,
        T fallback)
    {
        try
        {
            return await rpc();
        }
        catch
        {
            return fallback;
        }
    }

    private static LocationResponse ToLocationResponse(
        GetBusinessLocationResponse? location)
    {
        if (location is null)
            return EmptyLocation();

        return new LocationResponse(
            FullAddress: location.FullAddress,
            City: location.City,
            Street: location.Street,
            House: location.House,
            Latitude: location.Latitude.ToString(
                CultureInfo.InvariantCulture),
            Longitude: location.Longitude.ToString(
                CultureInfo.InvariantCulture));
    }

    private static LocationResponse ToLocationResponse(
        LocationDTO? location)
    {
        if (location is null)
            return EmptyLocation();

        return new LocationResponse(
            FullAddress: location.FullAddress ?? string.Empty,
            City: location.City ?? string.Empty,
            Street: location.Street ?? string.Empty,
            House: location.House ?? string.Empty,
            Latitude: location.Latitude.ToString(
                CultureInfo.InvariantCulture),
            Longitude: location.Longitude.ToString(
                CultureInfo.InvariantCulture));
    }

    private static LocationResponse EmptyLocation()
    {
        return new LocationResponse(
            FullAddress: string.Empty,
            City: string.Empty,
            Street: string.Empty,
            House: string.Empty,
            Latitude: string.Empty,
            Longitude: string.Empty);
    }

    private static OrderStatus ParseOrderStatus(
        DF.Contracts.Enums.OrderStatus status)
    {
        var numeric = (int)status;

        if (Enum.IsDefined(typeof(OrderStatus), numeric))
            return (OrderStatus)numeric;

        throw new OrderStateException(
            $"Unsupported order status '{status}'");
    }
}