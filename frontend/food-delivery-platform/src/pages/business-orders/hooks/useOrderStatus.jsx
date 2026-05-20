import { changeOrderStatus } from "../../../api/Order.ts";
import {OrderStatus} from "../../../models/enums/OrderStatus.ts";

const FRONT_TO_BACK_STATUS = {
    preparing: OrderStatus.Preparing,
    ready: OrderStatus.Ready,
    "picked-up": OrderStatus.PickedUp,
    delivered: OrderStatus.Delivered,
    cancelled: OrderStatus.Canceled
};

export function useOrderStatus(setOrders) {
    const handleStatusChange = async (orderId, newStatus) => {
        try {
            const backendStatus = FRONT_TO_BACK_STATUS[newStatus];
            await changeOrderStatus(orderId, backendStatus);

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