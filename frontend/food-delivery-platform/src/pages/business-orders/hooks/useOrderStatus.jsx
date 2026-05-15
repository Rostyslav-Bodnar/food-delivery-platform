import { changeOrderStatus } from "../../../api/Order.ts";

const FRONT_TO_BACK_STATUS = {
    preparing: "Preparing",
    ready: "Ready",
    delivered: "Delivered",
    cancelled: "Canceled"
};

export function useOrderStatus(setOrders) {
    const handleStatusChange = async (orderId, newStatus) => {
        try {
            await changeOrderStatus(orderId, FRONT_TO_BACK_STATUS[newStatus]);

            setOrders(prev =>
                prev.map(o =>
                    o.id === orderId
                        ? { ...o, status: newStatus }
                        : o
                )
            );
        } catch (e) {
            console.error("Failed to change order status", e);
        }
    };

    return { handleStatusChange };
}