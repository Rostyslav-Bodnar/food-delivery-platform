using DF.Contracts.EventDriven;
using DF.Contracts.RPC.Requests.MenuService;
using DF.Contracts.RPC.Requests.UserService;
using DF.Contracts.RPC.Responses.UserService;
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
    UserServiceRpcClient userServiceRpcClient,
    IDistanceService distanceService,
    MenuServiceRpcClient menuServiceRpcClient,
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

        // 3️⃣ Створюємо ордер
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
            Profit = 0
        };

        await orderRepository.Create(order);

        // 4️⃣ Публікуємо подію
        var evt = new OrderCreatedEvent(
            OrderId: order.Id,
            BusinessId: order.BusinessId,
            OrderedBy: order.OrderedBy,
            OrderDate: order.OrderDate,
            TotalPrice: order.TotalPrice,
            DeliverTo: new LocationDto(
                request.DeliverTo.FullAddress
            ),
            DeliverFrom: new LocationDto(
                request.DeliverFrom.FullAddress
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

    public async Task<OrderDetailsResponse> GetOrderAsync(Guid orderId)
    {
        var order = await orderRepository.Get(orderId)
                     ?? throw new InvalidOperationException($"Order {orderId} not found");

        // 1️⃣ Business
        var businessTask = userServiceRpcClient.GetBusinessAccountAsync(
            new GetBusinessAccountRequest(order.BusinessId));

        // 2️⃣ Customer
        var customerTask = userServiceRpcClient.GetCustomerAccountAsync(
            new GetCustomerAccountRequest(order.OrderedBy));

        // 3️⃣ Courier (optional)
        Task<GetCourierAccountResponse?> courierTask = order.DeliveredById != null
            ? userServiceRpcClient.GetCourierAccountAsync(
                new GetCourierAccountRequest(order.DeliveredById.Value))
            : Task.FromResult<GetCourierAccountResponse?>(null);

        // 4️⃣ Dishes from MenuService
        var dishesTask = menuServiceRpcClient.GetDishesAsync(
            new GetDishesRequest(order.Id));

        await Task.WhenAll(businessTask, customerTask, courierTask, dishesTask);

        var business = await businessTask;
        var customer = await customerTask;
        var courier = await courierTask;
        var dishesResponse = await dishesTask;

        // 5️⃣ Map dishes
        var dishDtos = dishesResponse.Dishes.Select(d =>
            new DishResponse(
                Id: d.DishId,
                BusinessId: d.BusinessId,
                DishName: d.Name,
                Quantity: 1,
                Price: d.Price
            )
        ).ToList();

        // 6️⃣ Build response
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
        var orders = (await orderRepository.GetAll())
            .Where(o => o.OrderStatus != OrderStatus.Canceled && o.OrderStatus != OrderStatus.Delivered)
            .ToList();

        if (!orders.Any())
            return Enumerable.Empty<BusinessOrderResponse>();

        // 1️⃣ Business
        var businessTask = userServiceRpcClient.GetBusinessAccountAsync(
            new GetBusinessAccountRequest(businessId));

        // 2️⃣ Couriers (optional)
        var courierTasks = orders
            .Where(o => o.DeliveredById != null)
            .Select(o => o.DeliveredById!.Value)
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

        // 6️⃣ Map orders + dishes
        var responses = new List<BusinessOrderResponse>();

        foreach (var o in orders)
        {
            var courier = o.DeliveredById != null
                ? couriers.GetValueOrDefault(o.DeliveredById.Value)
                : null;

            var customer = customers[o.OrderedBy];

            // 🔑 Отримуємо OrderedDishes для цього ордера
            var orderedDishes = await orderDishRepository.GetOrderDishesByOrderId(o.Id);

            var dishResponses = new List<DishResponse>();

            foreach (var od in orderedDishes)
            {
                // RPC виклик до MenuService для деталей страви
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
                CustomerFullName: $"{customer.Name} {customer.Surname}",
                CustomerAddress: customer.Address,
                OrderDate: o.OrderDate,
                TotalPrice: o.TotalPrice,
                DeliveredBy: o.DeliveredById ?? Guid.Empty,
                CourierName: courier != null
                    ? $"{courier.Name} {courier.Surname}"
                    : string.Empty,
                OrderStatus: o.OrderStatus.ToString(),
                dishes: dishResponses
            ));
        }

        return responses;
    }

    
    public async Task<IEnumerable<CustomerOrderResponse>> GetAllByCustomerIdAsync(Guid customerId)
    {
        var orders = (await orderRepository.GetAll())
            .Where(o => o.OrderStatus != OrderStatus.Canceled && o.OrderStatus != OrderStatus.Delivered)
            .ToList();

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
            .Where(o => o.DeliveredById != null)
            .Select(o => o.DeliveredById!.Value)
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

        // 5️⃣ DTO generation з отриманням страв
        var responses = new List<CustomerOrderResponse>();

        foreach (var order in orders)
        {
            var business = businesses[order.BusinessId];
            var courier = order.DeliveredById != null
                ? couriers.GetValueOrDefault(order.DeliveredById.Value)
                : null;

            // 🔑 Отримуємо OrderedDishes для цього ордера
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
                BusinessAddress: business.Adresses.FirstOrDefault() ?? string.Empty,
                OrderedBy: order.OrderedBy,
                OrderDate: order.OrderDate,
                TotalPrice: order.TotalPrice,
                DeliveredBy: order.DeliveredById ?? Guid.Empty,
                CourierName: courier != null
                    ? $"{courier.Name} {courier.Surname}"
                    : string.Empty,
                OrderStatus: order.OrderStatus.ToString(),
                dishes: dishResponses
            ));
        }

        return responses;
    }

    
    public async Task<IEnumerable<CourierOrderResponse>> GetAllByCourierIdAsync(Guid courierId)
    {
        var orders = (await orderRepository.GetAll())
            .Where(o => o.OrderStatus != OrderStatus.Canceled && o.OrderStatus != OrderStatus.Delivered)
            .ToList();


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
                CustomerPhoneNumber: customer.PhoneNumber,
                OrderDate: o.OrderDate,
                TotalPrice: o.TotalPrice,
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

        var customerTask = userServiceRpcClient.GetCustomerAccountAsync(new GetCustomerAccountRequest(customerId));

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

        await Task.WhenAll(businessTasks.Values.Concat<Task>(courierTasks.Values).Append(customerTask));

        var customer = await customerTask;
        var businesses = businessTasks.ToDictionary(x => x.Key, x => x.Value.Result);
        var couriers = courierTasks.ToDictionary(x => x.Key, x => x.Value.Result);

        var responses = new List<CustomerOrderResponse>();

        foreach (var order in orders)
        {
            var business = businesses[order.BusinessId];
            var courier = order.DeliveredById != null ? couriers.GetValueOrDefault(order.DeliveredById.Value) : null;

            // 🔑 Отримуємо OrderedDishes для цього ордера
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
                BusinessAddress: business.Adresses.FirstOrDefault() ?? string.Empty,
                OrderedBy: order.OrderedBy,
                OrderDate: order.OrderDate,
                TotalPrice: order.TotalPrice,
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

        var customerTasks = orders
            .Select(o => o.OrderedBy)
            .Distinct()
            .ToDictionary(
                id => id,
                id => userServiceRpcClient.GetCustomerAccountAsync(new GetCustomerAccountRequest(id))
            );

        var allTasks = businessTasks.Values.Select(t => (Task)t)
            .Concat(customerTasks.Values.Select(t => (Task)t))
            .Append(courierTask);

        await Task.WhenAll(allTasks);

        var courier = await courierTask;
        var businesses = businessTasks.ToDictionary(x => x.Key, x => x.Value.Result);
        var customers = customerTasks.ToDictionary(x => x.Key, x => x.Value.Result);

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
                CustomerPhoneNumber: customer.PhoneNumber,
                OrderDate: o.OrderDate,
                TotalPrice: o.TotalPrice,
                OrderStatus: o.OrderStatus.ToString(),
                Profit: o.Profit
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

    public async Task<OrderResponse> DeliverOrderAsync(Guid orderId, Guid courierId)
    {
        var order = await orderRepository.Get(orderId);
        
        if(order is null)
            throw new InvalidOperationException($"Order {orderId} not found");
        
        order.DeliveredById = courierId;
        order.OrderStatus  = OrderStatus.OutForDelivery;
        
        await orderRepository.Update(order);
        
        var business = await userServiceRpcClient.GetBusinessAccountAsync( new GetBusinessAccountRequest(order.BusinessId));
        
        return new OrderResponse( 
            Id: order.Id, 
            BusinessId: 
            order.BusinessId, 
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