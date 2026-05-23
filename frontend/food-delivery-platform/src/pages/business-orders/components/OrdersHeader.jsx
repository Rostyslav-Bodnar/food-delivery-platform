import React from "react";
import OrdersFilterBar from "../../../global-components/orders-filter/OrdersFilterBar.jsx";

// Terminal statuses (Delivered/Cancelled) live on the dedicated history page
// — the backend's active feed already filters them out, so listing them as
// chips here would always show 0 results.
const STATUSES = [
    { key: "all",          label: "All" },
    { key: "pending",      label: "New",        color: "#7c5cff" },
    { key: "preparing",    label: "Preparing",  color: "#ffb86b" },
    { key: "ready",        label: "Ready",      color: "#00d4ff" },
    { key: "on-the-way",   label: "On the way", color: "#00d4ff" },
    { key: "picked-up",    label: "Picked up",  color: "#50fa7b" }
];

const SORT_OPTIONS = [
    { value: "newest",     label: "Newest first" },
    { value: "oldest",     label: "Oldest first" },
    { value: "total-desc", label: "Total: high to low" },
    { value: "total-asc",  label: "Total: low to high" }
];

export default function OrdersHeader({
    filter, setFilter,
    sort, setSort,
    search, setSearch,
    count
}) {
    return (
        <>
            <header className="bh-top">
                <h1 className="bh-heading">Orders</h1>
            </header>

            <OrdersFilterBar
                statuses={STATUSES}
                activeStatus={filter}
                onStatusChange={setFilter}
                sortOptions={SORT_OPTIONS}
                sort={sort}
                onSortChange={setSort}
                search={search}
                onSearchChange={setSearch}
                searchPlaceholder="Search by order id or customer…"
                count={count}
            />
        </>
    );
}
