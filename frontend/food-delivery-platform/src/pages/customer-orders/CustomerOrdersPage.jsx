import React from "react";
import {
    Bike,
    CheckCircle2,
    Clock3,
    Package,
    XCircle
} from "lucide-react";

import "./styles/CustomerOrdersPage.css";
import CustomerSidebar from "../sidebars/CustomerSidebar";
import OrderDetailsComponent from "../order-details-modal/OrderDetailsComponent.jsx";
import CustomerOrdersHeader from "./components/CustomerOrdersHeader";
import CustomerOrdersContent from "./components/CustomerOrdersContent";
import CancelOrderModal from "./components/CancelOrderModal.jsx";
import { useCustomerOrders } from "./hooks/useCustomerOrders";
import { useCustomerOrderSelection } from "./hooks/useCustomerOrderSelection";
import { useCustomerOrderStatusMeta } from "./hooks/useCustomerOrderStatusMeta";
import LiveOrderTrackingModal from "../../features/order-tracking/LiveOrderTrackingModal.jsx";

const CustomerOrdersPage = () => {
    const customerId = localStorage.getItem("currentAccountId");

    const {
        orders,
        loading,
        error,
        cancellingOrderId,
        cancelCustomerOrder
    } = useCustomerOrders(customerId);
    const {
        selectedOrder,
        openOrderDetails,
        closeOrderDetails
    } = useCustomerOrderSelection();
    const { getStatusMeta } = useCustomerOrderStatusMeta();

    const [orderToCancel, setOrderToCancel] = React.useState(null);
    const [trackingOrder, setTrackingOrder] = React.useState(null);

    const confirmCancelOrder = async () => {
        if (!orderToCancel) {
            return;
        }

        try {
            await cancelCustomerOrder(orderToCancel.id);
            if (selectedOrder?.id === orderToCancel.id) {
                closeOrderDetails();
            }
            if (trackingOrder?.id === orderToCancel.id) {
                setTrackingOrder(null);
            }
            setOrderToCancel(null);
        } catch (cancelError) {
            alert(cancelError.message);
        }
    };

    return (
        <div className="app-wrapper">
            <CustomerSidebar />

            <main className="auth-homepage customer-orders-page">
                <CustomerOrdersHeader />

                <CustomerOrdersContent
                    loading={loading}
                    error={error}
                    orders={orders}
                    getStatusMeta={getStatusMeta}
                    onOpenDetails={openOrderDetails}
                    onTrackOrder={setTrackingOrder}
                    onRequestCancel={setOrderToCancel}
                    cancellingOrderId={cancellingOrderId}
                />
            </main>

            {selectedOrder && (
                <OrderDetailsComponent
                    order={selectedOrder}
                    statusMap={{
                        preparing: { label: "Preparing", icon: Clock3 },
                        "on-the-way": { label: "On the way", icon: Bike },
                        new: { label: "New", icon: Package },
                        cancelled: { label: "Cancelled", icon: XCircle },
                        delivered: { label: "Delivered", icon: CheckCircle2 }
                    }}
                    onClose={closeOrderDetails}
                />
            )}

            {orderToCancel && (
                <CancelOrderModal
                    order={orderToCancel}
                    cancelling={cancellingOrderId === orderToCancel.id}
                    onClose={() => setOrderToCancel(null)}
                    onConfirm={confirmCancelOrder}
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
};

export default CustomerOrdersPage;
