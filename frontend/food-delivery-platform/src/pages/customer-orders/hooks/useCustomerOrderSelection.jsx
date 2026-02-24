import { useState } from "react";

export function useCustomerOrderSelection() {
    const [selectedOrder, setSelectedOrder] = useState(null);

    const openOrderDetails = (order) => setSelectedOrder(order);
    const closeOrderDetails = () => setSelectedOrder(null);

    return {
        selectedOrder,
        openOrderDetails,
        closeOrderDetails
    };
}