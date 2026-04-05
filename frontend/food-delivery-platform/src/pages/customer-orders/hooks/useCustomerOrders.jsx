import { useCallback, useEffect, useState } from "react";
import {
    cancelOrder,
    getCustomerOrders,
    getOrderDetails
} from "../../../api/Order.jsx";
import { buildLocation, formatLocation, hasCoordinates } from "../../../utils/orderLocations.js";

const mapStatus = (status) => {
    switch (status) {
        case "Preparing":
            return "preparing";
        case "OnTheWay":
            return "on-the-way";
        case "Cancelled":
            return "cancelled";
        case "Delivered":
            return "delivered";
        default:
            return "new";
    }
};

const toCoords = (location) =>
    hasCoordinates(location)
        ? {
            latitude: location.latitude,
            longitude: location.longitude
        }
        : null;

const mapOrder = (order) => {
    const businessLocation = buildLocation(order, "business");
    const customerLocation = buildLocation(order, "customer");
    const courierLocation = buildLocation(order, "courier");

    return {
        id: order.id,
        businessId: order.businessId,
        restaurant: order.businessName,
        address: formatLocation(businessLocation),
        businessLocation,
        customerLocation,
        courierLocation,
        businessCoords: toCoords(businessLocation),
        customerCoords: toCoords(customerLocation),
        courierCoords: toCoords(courierLocation),
        status: mapStatus(order.orderStatus),
        rawStatus: order.orderStatus,
        total: order.totalPrice,
        createdAt: new Date(order.orderDate).toLocaleString(),
        createdAtRaw: order.orderDate,
        courier: order.courierName ? { name: order.courierName } : null,
        canCancel: ["New", "Preparing"].includes(order.orderStatus),
        items: order.dishes.map((dish) => ({
            id: dish.id,
            name: dish.dishName,
            quantity: dish.quantity,
            price: dish.price
        }))
    };
};

export function useCustomerOrders(customerId) {
    const [orders, setOrders] = useState([]);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState("");
    const [cancellingOrderId, setCancellingOrderId] = useState(null);

    const loadOrders = useCallback(async () => {
        if (!customerId) {
            setOrders([]);
            setLoading(false);
            return;
        }

        setLoading(true);
        setError("");

        try {
            const data = await getCustomerOrders(customerId);
            setOrders(data.map(mapOrder));
        } catch (loadError) {
            console.error("Failed to load orders", loadError);
            setError("Failed to load active orders.");
        } finally {
            setLoading(false);
        }
    }, [customerId]);

    useEffect(() => {
        loadOrders();
    }, [loadOrders]);

    const cancelCustomerOrder = async (orderId) => {
        setCancellingOrderId(orderId);
        setError("");

        try {
            await cancelOrder(orderId);
            await loadOrders();
        } catch (cancelError) {
            console.error("Failed to cancel order", cancelError);
            throw new Error("Failed to cancel order.");
        } finally {
            setCancellingOrderId(null);
        }
    };

    const getTrackingDetails = async (orderId) => {
        const details = await getOrderDetails(orderId);

        return {
            id: details.id,
            customerAddress: details.customerAddress,
            customerPhoneNumber: details.customerPhoneNumber,
            customerFullName: details.customerFullName,
            courierName: details.courierName,
            courierPhoneNumber: details.courierPhoneNumber,
            deliveredById: details.deliveredById,
            orderStatus: mapStatus(details.orderStatus),
            rawStatus: details.orderStatus
        };
    };

    return {
        orders,
        loading,
        error,
        cancellingOrderId,
        reloadOrders: loadOrders,
        cancelCustomerOrder,
        getTrackingDetails
    };
}
