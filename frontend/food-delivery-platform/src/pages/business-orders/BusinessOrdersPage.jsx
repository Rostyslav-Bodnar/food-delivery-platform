import React from "react";
import {
    Package,
    Clock,
    CheckCircle,
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

const STATUS_MAP = {
    pending: { label: "New", color: "#7c5cff", icon: Package },
    preparing: { label: "Preparing", color: "#ffb86b", icon: Clock },
    ready: { label: "Ready", color: "#00d4ff", icon: CheckCircle },
    delivered: { label: "Delivered", color: "#50fa7b", icon: CheckCircle },
    cancelled: { label: "Cancelled", color: "#ff6b6b", icon: XCircle },
};

export default function BusinessOrdersPage({ userData }) {
    const businessId = localStorage.getItem("currentAccountId");

    const { orders, setOrders, loading } = useBusinessOrders(businessId);
    const { filter, setFilter, filteredOrders } = useOrderFilter(orders);
    const { selectedOrder, openOrder, closeOrder } = useOrderSelection();
    const { handleStatusChange } = useOrderStatus(setOrders);

    return (
        <div className="bh-page">
            <BusinessSidebar userData={userData} />

            <main className="bh-main">
                <OrdersHeader filter={filter} setFilter={setFilter} />

                <section className="bh-content">
                    <OrdersContent
                        loading={loading}
                        filteredOrders={filteredOrders}
                        statusMap={STATUS_MAP}
                        onStatusChange={handleStatusChange}
                        onOpenDetails={openOrder}
                    />
                </section>
            </main>

            {selectedOrder && (
                <OrderDetailsComponent
                    order={selectedOrder}
                    statusMap={STATUS_MAP}
                    onClose={closeOrder}
                />
            )}
        </div>
    );
}