import React from "react";
import OrdersFilterBar from "../../../global-components/orders-filter/OrdersFilterBar.jsx";

const STATUSES = [
    { key: "all",         label: "All" },
    { key: "preparing",   label: "Preparing",  color: "#ffb86b" },
    { key: "ready",       label: "Ready",      color: "#00d4ff" },
    { key: "on-the-way",  label: "On the way", color: "#00d4ff" },
    { key: "picked-up",   label: "Picked up",  color: "#4bd68a" },
    { key: "delivered",   label: "Delivered",  color: "#4bd68a" },
    { key: "cancelled",   label: "Cancelled",  color: "#ff8f8f" }
];

const SORT_OPTIONS = [
    { value: "newest",     label: "Newest first" },
    { value: "oldest",     label: "Oldest first" },
    { value: "total-desc", label: "Total: high to low" },
    { value: "total-asc",  label: "Total: low to high" }
];

export default function CustomerOrdersHeader({
    filter, setFilter,
    sort, setSort,
    search, setSearch,
    count
}) {
    return (
        <div className="customer-orders-page-header">
            <h1 className="gradient-title">My Orders</h1>

            <OrdersFilterBar
                statuses={STATUSES}
                activeStatus={filter}
                onStatusChange={setFilter}
                sortOptions={SORT_OPTIONS}
                sort={sort}
                onSortChange={setSort}
                search={search}
                onSearchChange={setSearch}
                searchPlaceholder="Search by restaurant…"
                count={count}
            />
        </div>
    );
}
