import { useMemo, useState } from "react";

export function useOrderFilter(orders) {
    const [filter, setFilter] = useState("all");

    const filteredOrders = useMemo(() => {
        return filter === "all"
            ? orders
            : orders.filter(o => o.status === filter);
    }, [orders, filter]);

    return { filter, setFilter, filteredOrders };
}