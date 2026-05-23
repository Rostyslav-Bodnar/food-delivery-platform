import { useMemo, useState } from "react";

const compareDate = (a, b) =>
    new Date(b.createdAtRaw ?? b.orderDate ?? 0) - new Date(a.createdAtRaw ?? a.orderDate ?? 0);

const sortStrategies = {
    "newest": (orders) => [...orders].sort(compareDate),
    "oldest": (orders) => [...orders].sort((a, b) => -compareDate(a, b)),
    "total-desc": (orders) => [...orders].sort((a, b) => Number(b.total ?? 0) - Number(a.total ?? 0)),
    "total-asc": (orders) => [...orders].sort((a, b) => Number(a.total ?? 0) - Number(b.total ?? 0))
};

export function useCustomerOrderFilters(orders) {
    const [filter, setFilter] = useState("all");
    const [sort, setSort] = useState("newest");
    const [search, setSearch] = useState("");

    const filteredOrders = useMemo(() => {
        let list = filter === "all"
            ? orders
            : orders.filter((o) => o.status === filter);

        const query = search.trim().toLowerCase();
        if (query) {
            list = list.filter((o) =>
                String(o.restaurant ?? "").toLowerCase().includes(query)
                || String(o.id ?? "").toLowerCase().includes(query)
            );
        }

        const sortFn = sortStrategies[sort] ?? sortStrategies.newest;
        return sortFn(list);
    }, [orders, filter, sort, search]);

    return {
        filter, setFilter,
        sort, setSort,
        search, setSearch,
        filteredOrders
    };
}
