import { useCallback, useEffect, useState } from "react";
import {
    cancelOrder,
    getCustomerOrders
} from "../../../api/Order.ts";
import { buildLocation, formatLocation, hasCoordinates } from "../../../utils/orderLocations.js";

const mapStatus = (status) => {
    switch (status) {
        case "Preparing":
            return "preparing";
        case "Ready":
            return "ready";
        case "OnTheWay":
        case "OutForDelivery":
            return "on-the-way";
        case "PickedUp":
            return "picked-up";
        case "Cancelled":
        case "Canceled":
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
        deliveryMethod: order.deliveryMethod ?? "Delivery",
        paymentMethod: order.paymentMethod ?? "",
        courierPaid: Boolean(order.courierPaid),
        isPaid: Boolean(order.isPaid),
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

    const loadOrders = useCallback(async ({ silent = false } = {}) => {
        if (!customerId) {
            setOrders([]);
            setLoading(false);
            return;
        }

        if (!silent) setLoading(true);
        if (!silent) setError("");

        try {
            const data = await getCustomerOrders(customerId);
            setOrders(data.map(mapOrder));
        } catch (loadError) {
            console.error("Failed to load orders", loadError);
            if (!silent) setError("Failed to load active orders.");
        } finally {
            if (!silent) setLoading(false);
        }
    }, [customerId]);

    useEffect(() => {
        loadOrders();
        const intervalId = window.setInterval(
            () => loadOrders({ silent: true }),
            30000
        );
        return () => window.clearInterval(intervalId);
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

    return {
        orders,
        loading,
        error,
        cancellingOrderId,
        reloadOrders: loadOrders,
        cancelCustomerOrder
    };
}
