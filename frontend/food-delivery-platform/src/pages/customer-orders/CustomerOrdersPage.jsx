import React from "react";

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
import { changeOrderStatus } from "../../api/Order.ts";
import { OrderStatus } from "../../models/enums/OrderStatus.ts";
import useOrderEventsSubscription from "../../hooks/useOrderEventsSubscription.jsx";

const CustomerOrdersPage = () => {
    const customerId = localStorage.getItem("currentAccountId");

    const {
        orders,
        loading,
        error,
        cancellingOrderId,
        cancelCustomerOrder,
        reloadOrders
    } = useCustomerOrders(customerId);

    useOrderEventsSubscription({
        enabled: Boolean(customerId),
        onStatusChanged: (evt) => {
            if (evt?.customerId === customerId) {
                reloadOrders?.({ silent: true });
            }
        },
        onCourierPaid: (evt) => {
            if (evt?.customerId === customerId) {
                reloadOrders?.({ silent: true });
            }
        },
        onReconnected: () => reloadOrders?.({ silent: true })
    });
    const {
        selectedOrder,
        openOrderDetails,
        closeOrderDetails
    } = useCustomerOrderSelection();
    const { getStatusMeta } = useCustomerOrderStatusMeta();

    const [orderToCancel, setOrderToCancel] = React.useState(null);
    const [trackingOrder, setTrackingOrder] = React.useState(null);
    const [confirmingDeliveryId, setConfirmingDeliveryId] = React.useState(null);

    const confirmDelivered = async (order) => {
        try {
            setConfirmingDeliveryId(order.id);
            await changeOrderStatus(order.id, OrderStatus.Delivered);
            // Polling will move the order out of active and into history.
        } catch (err) {
            console.error("Failed to confirm delivery", err);
            alert(err.message ?? "Failed to confirm delivery");
        } finally {
            setConfirmingDeliveryId(null);
        }
    };

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
                    onConfirmDelivered={confirmDelivered}
                    confirmingDeliveryId={confirmingDeliveryId}
                    cancellingOrderId={cancellingOrderId}
                />
            </main>

            {selectedOrder && (
                <OrderDetailsComponent
                    orderId={selectedOrder.id}
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
