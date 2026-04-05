export function useCustomerOrderStatusMeta() {
    const getStatusMeta = (status) => {
        if (status === "preparing") {
            return { text: "Preparing", color: "#ffb86b", icon: "🍳" };
        }

        if (status === "on-the-way") {
            return { text: "On the way", color: "#00d4ff", icon: "🏍️" };
        }

        if (status === "delivered") {
            return { text: "Delivered", color: "#4bd68a", icon: "✅" };
        }

        if (status === "cancelled") {
            return { text: "Cancelled", color: "#ff8f8f", icon: "✕" };
        }

        return { text: "New", color: "#7c5cff", icon: "📦" };
    };

    return { getStatusMeta };
}
