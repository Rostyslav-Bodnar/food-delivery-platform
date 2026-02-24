import { useEffect, useState } from "react";
import { getOrdersByBusiness } from "../../../api/Order.jsx";

const BACKEND_STATUS_MAP = {
    Preparing: "preparing",
    Ready: "ready",
    OutForDelivery: "ready",
    Delivered: "delivered",
    Canceled: "cancelled"
};

export function useBusinessOrders(businessId) {
    const [orders, setOrders] = useState([]);
    const [loading, setLoading] = useState(true);

    useEffect(() => {
        if (!businessId) return;

        const loadOrders = async () => {
            try {
                const data = await getOrdersByBusiness(businessId);

                const mapped = data.map(o => ({
                    id: o.id,
                    createdAt: new Date(o.orderDate).toLocaleTimeString([], {
                        hour: "2-digit",
                        minute: "2-digit"
                    }),
                    customerName: o.customerFullName,
                    address: o.customerAddress,
                    total: o.totalPrice,
                    status: BACKEND_STATUS_MAP[o.orderStatus] ?? "pending",
                    courier: o.courierName ? { name: o.courierName } : null,
                    items: o.dishes.map(d => ({
                        name: d.dishName,
                        quantity: d.quantity,
                        price: d.price
                    }))
                }));

                setOrders(mapped);
            } catch (e) {
                console.error("Failed to load business orders", e);
            } finally {
                setLoading(false);
            }
        };

        loadOrders();
    }, [businessId]);

    return { orders, setOrders, loading };
}