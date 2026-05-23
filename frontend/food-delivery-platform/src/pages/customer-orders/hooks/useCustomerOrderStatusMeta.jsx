import React from "react";
import {
    Bike,
    CheckCircle2,
    Clock3,
    Package,
    Store,
    XCircle
} from "lucide-react";

export function useCustomerOrderStatusMeta() {
    const getStatusMeta = (status, deliveryMethod) => {
        const isPickup = deliveryMethod === "Pickup";

        if (status === "preparing") {
            return { text: "Preparing", color: "#ffb86b", icon: <Clock3 size={15} /> };
        }

        if (status === "ready") {
            return isPickup
                ? { text: "Ready to collect", color: "#4bd68a", icon: <Store size={15} /> }
                : { text: "Awaiting courier", color: "#00d4ff", icon: <Clock3 size={15} /> };
        }

        if (status === "on-the-way") {
            return { text: "On the way", color: "#00d4ff", icon: <Bike size={15} /> };
        }

        if (status === "picked-up") {
            return { text: "Picked up", color: "#4bd68a", icon: <Bike size={15} /> };
        }

        if (status === "delivered") {
            return { text: "Delivered", color: "#4bd68a", icon: <CheckCircle2 size={15} /> };
        }

        if (status === "cancelled") {
            return { text: "Cancelled", color: "#ff8f8f", icon: <XCircle size={15} /> };
        }

        return { text: "New", color: "#7c5cff", icon: <Package size={15} /> };
    };

    return { getStatusMeta };
}
