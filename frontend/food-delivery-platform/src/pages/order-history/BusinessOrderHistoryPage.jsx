import React, { useCallback, useEffect, useMemo, useState } from "react";
import { CheckCircle2, History, XCircle } from "lucide-react";

import "./styles/OrderHistoryPage.css";
import RoleSidebar from "../sidebars/RoleSidebar.jsx";
import OrdersFilterBar from "../../global-components/orders-filter/OrdersFilterBar.jsx";
import OrderDetailsComponent from "../order-details-modal/OrderDetailsComponent.jsx";
import OrderHistoryCard from "./components/OrderHistoryCard.jsx";

import { getBusinessOrderHistory } from "../../api/Order.ts";
import { buildLocation, formatLocation } from "../../utils/orderLocations.js";

const STATUS_META = {
    delivered: { text: "Delivered", color: "#4bd68a", icon: <CheckCircle2 size={14} /> },
    cancelled: { text: "Cancelled", color: "#ff8f8f", icon: <XCircle size={14} /> }
};

const STATUS_FILTERS = [
    { key: "all", label: "All" },
    { key: "delivered", label: "Delivered", color: "#4bd68a" },
    { key: "cancelled", label: "Cancelled", color: "#ff8f8f" }
];

const SORT_OPTIONS = [
    { value: "newest", label: "Newest first" },
    { value: "oldest", label: "Oldest first" },
    { value: "total-desc", label: "Total: high to low" },
    { value: "total-asc", label: "Total: low to high" }
];

const mapStatus = (raw) => {
    switch (raw) {
        case "Delivered":  return "delivered";
        case "Canceled":
        case "Cancelled":  return "cancelled";
        default:           return null; // not a history status
    }
};

const mapOrder = (order) => {
    const customerLocation = buildLocation(order, "customer");
    return {
        id: order.id,
        businessId: order.businessId,
        restaurant: order.businessName,
        address: formatLocation(customerLocation),
        status: mapStatus(order.orderStatus),
        rawStatus: order.orderStatus,
        total: (Number(order.totalPrice ?? 0)) + (Number(order.deliveryFee ?? 0)),
        createdAt: new Date(order.orderDate).toLocaleString(),
        createdAtRaw: order.orderDate,
        items: (order.dishes ?? []).map((d) => ({
            id: d.id,
            name: d.dishName,
            quantity: d.quantity,
            price: d.price
        }))
    };
};

export default function BusinessOrderHistoryPage({ userData }) {
    const businessId = localStorage.getItem("currentAccountId");

    const [orders, setOrders] = useState([]);
    const [loading, setLoading] = useState(true);
    const [selectedOrder, setSelectedOrder] = useState(null);

    const [filter, setFilter] = useState("all");
    const [sort, setSort] = useState("newest");
    const [search, setSearch] = useState("");

    const load = useCallback(async () => {
        if (!businessId) {
            setOrders([]);
            setLoading(false);
            return;
        }
        try {
            const data = await getBusinessOrderHistory(businessId);
            setOrders((data ?? []).map(mapOrder).filter((o) => o.status !== null));
        } catch (err) {
            console.error("Failed to load business order history", err);
        } finally {
            setLoading(false);
        }
    }, [businessId]);

    useEffect(() => {
        load();
    }, [load]);

    const filtered = useMemo(() => {
        let list = filter === "all" ? orders : orders.filter((o) => o.status === filter);

        const q = search.trim().toLowerCase();
        if (q) {
            list = list.filter((o) =>
                String(o.id ?? "").toLowerCase().includes(q)
                || String(o.address ?? "").toLowerCase().includes(q)
            );
        }

        const cmpDate = (a, b) =>
            new Date(b.createdAtRaw ?? 0) - new Date(a.createdAtRaw ?? 0);
        const cmpTotal = (a, b) => Number(b.total ?? 0) - Number(a.total ?? 0);

        const sorted = [...list];
        switch (sort) {
            case "oldest":     sorted.sort((a, b) => -cmpDate(a, b)); break;
            case "total-desc": sorted.sort(cmpTotal); break;
            case "total-asc":  sorted.sort((a, b) => -cmpTotal(a, b)); break;
            case "newest":
            default:           sorted.sort(cmpDate); break;
        }
        return sorted;
    }, [orders, filter, sort, search]);

    return (
        <div className="app-wrapper">
            <RoleSidebar userData={userData} />

            <main className="order-history-page">
                <div className="order-history-inner">
                    <header className="order-history-header">
                        <h1 className="order-history-title">
                            <History size={28} style={{ verticalAlign: "middle", marginRight: 10 }} />
                            Order History
                        </h1>

                        <OrdersFilterBar
                            statuses={STATUS_FILTERS}
                            activeStatus={filter}
                            onStatusChange={setFilter}
                            sortOptions={SORT_OPTIONS}
                            sort={sort}
                            onSortChange={setSort}
                            search={search}
                            onSearchChange={setSearch}
                            searchPlaceholder="Search by order id or customer address…"
                            count={filtered.length}
                        />
                    </header>

                    {loading ? (
                        <div className="history-loading">Loading past orders…</div>
                    ) : filtered.length === 0 ? (
                        <div className="history-empty">
                            <div className="history-empty__icon">📦</div>
                            <h2>No past orders</h2>
                            <p>Completed and cancelled orders for this restaurant will appear here.</p>
                        </div>
                    ) : (
                        <div className="history-grid">
                            {filtered.map((order) => (
                                <OrderHistoryCard
                                    key={order.id}
                                    order={order}
                                    statusMeta={STATUS_META[order.status]}
                                    onOpenDetails={setSelectedOrder}
                                />
                            ))}
                        </div>
                    )}
                </div>
            </main>

            {selectedOrder && (
                <OrderDetailsComponent
                    orderId={selectedOrder.id}
                    onClose={() => setSelectedOrder(null)}
                />
            )}
        </div>
    );
}
