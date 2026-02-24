import { useState } from "react";

export function useOrderSelection() {
    const [selectedOrder, setSelectedOrder] = useState(null);

    const openOrder = order => setSelectedOrder(order);
    const closeOrder = () => setSelectedOrder(null);

    return { selectedOrder, openOrder, closeOrder };
}