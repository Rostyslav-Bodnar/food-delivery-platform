import { useCallback, useEffect, useState } from "react";
import { getOrdersByBusiness } from "../../../api/Order.ts";
import { buildLocation, formatLocation } from "../../../utils/orderLocations.js";

const BACKEND_STATUS_MAP = {
    Preparing: "preparing",
    Ready: "ready",
    OutForDelivery: "on-the-way",
    PickedUp: "picked-up",
    Delivered: "delivered",
    Canceled: "cancelled"
};

export function useBusinessOrders(businessId) {
    const [orders, setOrders] = useState([]);
    const [loading, setLoading] = useState(true);

    const reload = useCallback(async () => {
        if (!businessId) return;
        try {
            const data = await getOrdersByBusiness(businessId);

            const mapped = data.map((o) => {
                const businessLocation = buildLocation(o, "business");
                const customerLocation = buildLocation(o, "customer");
                const courierLocation = buildLocation(o, "courier");

                return {
                    id: o.id,
                    businessName: o.businessName,
                    restaurant: o.businessName,
                    createdAt: new Date(o.orderDate).toLocaleTimeString([], {
                        hour: "2-digit",
                        minute: "2-digit"
                    }),
                    customerName: "Customer",
                    address: formatLocation(customerLocation),
                    businessLocation,
                    customerLocation,
                    courierLocation,
                    businessAddress: formatLocation(businessLocation),
                    customerAddress: formatLocation(customerLocation),
                    total: o.totalPrice,
                    status: BACKEND_STATUS_MAP[o.orderStatus] ?? "pending",
                    rawStatus: o.orderStatus,
                    deliveryMethod: o.deliveryMethod ?? "Delivery",
                    paymentMethod: o.paymentMethod ?? "",
                    courier: o.courierName ? { name: o.courierName } : null,
                    items: o.dishes.map((d) => ({
                        name: d.dishName,
                        quantity: d.quantity,
                        price: d.price
                    }))
                };
            });

            setOrders(mapped);
        } catch (e) {
            console.error("Failed to load business orders", e);
        }
    }, [businessId]);

    useEffect(() => {
        if (!businessId) return;

        let isMounted = true;
        let firstLoad = true;

        const loadOrders = async () => {
            await reload();
            if (firstLoad && isMounted) {
                setLoading(false);
                firstLoad = false;
            }
        };

        loadOrders();
        const intervalId = window.setInterval(loadOrders, 30000);

        return () => {
            isMounted = false;
            window.clearInterval(intervalId);
        };
    }, [businessId, reload]);

    return { orders, setOrders, loading, reloadOrders: reload };
}
