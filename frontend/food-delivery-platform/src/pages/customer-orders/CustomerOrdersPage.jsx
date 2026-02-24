import React from "react";

import "./styles/CustomerOrdersPage.css";
import CustomerSidebar from "../sidebars/CustomerSidebar";
import OrderDetailsComponent from "../order-details-modal/OrderDetailsComponent.jsx";

import CustomerOrdersHeader from "./components/CustomerOrdersHeader";
import CustomerOrdersContent from "./components/CustomerOrdersContent";

import { useCustomerOrders } from "./hooks/useCustomerOrders";
import { useCustomerOrderSelection } from "./hooks/useCustomerOrderSelection";
import { useCustomerOrderStatusMeta } from "./hooks/useCustomerOrderStatusMeta";

const CustomerOrdersPage = () => {
    const customerId = localStorage.getItem("currentAccountId");

    const { orders, loading } = useCustomerOrders(customerId);
    const {
        selectedOrder,
        openOrderDetails,
        closeOrderDetails
    } = useCustomerOrderSelection();

    const { getStatusMeta } = useCustomerOrderStatusMeta();

    return (
        <div className="app-wrapper">
            <CustomerSidebar />

            <main className="auth-homepage customer-orders-page">
                <CustomerOrdersHeader />

                <CustomerOrdersContent
                    loading={loading}
                    orders={orders}
                    getStatusMeta={getStatusMeta}
                    onOpenDetails={openOrderDetails}
                />
            </main>

            {selectedOrder && (
                <OrderDetailsComponent
                    order={selectedOrder}
                    statusMap={{
                        preparing: { label: "Preparing", icon: () => <span>🍳</span> },
                        "on-the-way": { label: "On the way", icon: () => <span>🏍️</span> },
                        new: { label: "New", icon: () => <span>📦</span> }
                    }}
                    onClose={closeOrderDetails}
                />
            )}
        </div>
    );
};

export default CustomerOrdersPage;
