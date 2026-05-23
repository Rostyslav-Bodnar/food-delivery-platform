import React from "react";
import {
    Bike,
    CheckCircle,
    Clock,
    Package,
    XCircle
} from "lucide-react";

import "./styles/BusinessOrdersPage.css";
import BusinessSidebar from "../../pages/sidebars/BusinessSidebar.jsx";
import OrderDetailsComponent from "../order-details-modal/OrderDetailsComponent.jsx";
import OrdersHeader from "./components/OrdersHeader";
import OrdersContent from "./components/OrdersContent";
import { useBusinessOrders } from "./hooks/useBusinessOrders";
import { useOrderFilter } from "./hooks/useOrderFilter";
import { useOrderSelection } from "./hooks/useOrderSelection";
import { useOrderStatus } from "./hooks/useOrderStatus";
import LiveOrderTrackingModal from "../../features/order-tracking/LiveOrderTrackingModal.jsx";
import useOrderEventsSubscription from "../../hooks/useOrderEventsSubscription.jsx";

const STATUS_MAP = {
    pending: { label: "New", color: "#7c5cff", icon: Package },
    preparing: { label: "Preparing", color: "#ffb86b", icon: Clock },
    ready: { label: "Ready", color: "#00d4ff", icon: CheckCircle },
    "on-the-way": { label: "On the way", color: "#00d4ff", icon: Bike },
    "picked-up": { label: "Picked up", color: "#50fa7b", icon: Bike },
    delivered: { label: "Delivered", color: "#50fa7b", icon: CheckCircle },
    cancelled: { label: "Cancelled", color: "#ff6b6b", icon: XCircle }
};

export default function BusinessOrdersPage({ userData }) {
    const businessId = localStorage.getItem("currentAccountId");

    const { orders, setOrders, loading, reloadOrders } = useBusinessOrders(businessId);
    const {
        filter, setFilter,
        sort, setSort,
        search, setSearch,
        filteredOrders
    } = useOrderFilter(orders);
    const { selectedOrder, openOrder, closeOrder } = useOrderSelection();
    const { handleStatusChange } = useOrderStatus(setOrders);
    const [trackingOrder, setTrackingOrder] = React.useState(null);

    useOrderEventsSubscription({
        enabled: Boolean(businessId),
        onStatusChanged: (evt) => {
            if (evt?.businessId === businessId) {
                reloadOrders?.();
            }
        },
        onCourierPaid: (evt) => {
            if (evt?.businessId === businessId) {
                reloadOrders?.();
            }
        },
        onReconnected: () => reloadOrders?.()
    });

    return (
        <div className="bh-page">
            <BusinessSidebar userData={userData} />

            <main className="bh-main">
                <OrdersHeader
                    filter={filter}
                    setFilter={setFilter}
                    sort={sort}
                    setSort={setSort}
                    search={search}
                    setSearch={setSearch}
                    count={filteredOrders.length}
                />

                <section className="bh-content">
                    <OrdersContent
                        loading={loading}
                        filteredOrders={filteredOrders}
                        statusMap={STATUS_MAP}
                        onStatusChange={handleStatusChange}
                        onOpenDetails={openOrder}
                        onTrackOrder={setTrackingOrder}
                    />
                </section>
            </main>

            {selectedOrder && (
                <OrderDetailsComponent
                    orderId={selectedOrder.id}
                    onClose={closeOrder}
                />
            )}

            {trackingOrder && (
                <LiveOrderTrackingModal
                    order={trackingOrder}
                    onClose={() => setTrackingOrder(null)}
                />
            )}
        </div>
    );
}
