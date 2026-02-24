import { useEffect, useState } from "react";
import { getCustomerOrders } from "../../../api/Order.jsx";

const mapStatus = (status) => {
    switch (status) {
        case "Preparing": return "preparing";
        case "OnTheWay": return "on-the-way";
        default: return "new";
    }
};

export function useCustomerOrders(customerId) {
    const [orders, setOrders] = useState([]);
    const [loading, setLoading] = useState(true);

    useEffect(() => {
        if (!customerId) return;

        const loadOrders = async () => {
            try {
                const data = await getCustomerOrders(customerId);

                const mappedOrders = data.map(o => ({
                    id: o.id,
                    restaurant: o.businessName,
                    address: o.businessAddress,
                    status: mapStatus(o.orderStatus),
                    total: o.totalPrice,
                    createdAt: new Date(o.orderDate).toLocaleString(),
                    courier: o.courierName ? { name: o.courierName } : null,
                    items: o.dishes.map(d => ({
                        name: d.dishName,
                        quantity: d.quantity,
                        price: d.price
                    }))
                }));

                setOrders(mappedOrders);
            } catch (e) {
                console.error("Failed to load orders", e);
            } finally {
                setLoading(false);
            }
        };

        loadOrders();
    }, [customerId]);

    return { orders, loading };
}