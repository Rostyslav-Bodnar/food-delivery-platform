import React from "react";

import "./styles/CustomerOrdersPage.css";
import RoleSidebar from "../sidebars/RoleSidebar";
import OrderDetailsComponent from "../order-details-modal/OrderDetailsComponent.jsx";
import CustomerOrdersHeader from "./components/CustomerOrdersHeader";
import CustomerOrdersContent from "./components/CustomerOrdersContent";
import CancelOrderModal from "./components/CancelOrderModal.jsx";
import { useCustomerOrders } from "./hooks/useCustomerOrders";
import { useCustomerOrderSelection } from "./hooks/useCustomerOrderSelection";
import { useCustomerOrderStatusMeta } from "./hooks/useCustomerOrderStatusMeta";
import { useCustomerOrderFilters } from "./hooks/useCustomerOrderFilters";
import LiveOrderTrackingModal from "../../features/order-tracking/LiveOrderTrackingModal.jsx";
import { changeOrderStatus } from "../../api/Order.ts";
import { OrderStatus } from "../../models/enums/OrderStatus.ts";
import { getClientSecret, getPaymentByOrderId } from "../../api/Payment.jsx";
import useOrderEventsSubscription from "../../hooks/useOrderEventsSubscription.jsx";
import { useToast } from "../../global-components/toast/ToastContext";
import StripePaymentModal from "../checkout/components/StripePaymentModal.jsx";
import "../checkout/styles/StripePaymentModal.css";

const CustomerOrdersPage = () => {
    const toast = useToast();
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

    const {
        filter, setFilter,
        sort, setSort,
        search, setSearch,
        filteredOrders
    } = useCustomerOrderFilters(orders);

    const [orderToCancel, setOrderToCancel] = React.useState(null);
    const [trackingOrder, setTrackingOrder] = React.useState(null);
    const [confirmingDeliveryId, setConfirmingDeliveryId] = React.useState(null);

    // Resume-payment state. When the user dismisses the Stripe modal during
    // checkout, the Order row + PaymentIntent persist; the customer can
    // come back here and finish paying. `payingOrderId` shows the spinner
    // on the "Pay now" button while the clientSecret loads; the modal
    // opens once clientSecret is non-null.
    const [payingOrderId, setPayingOrderId] = React.useState(null);
    const [paymentForOrder, setPaymentForOrder] = React.useState(null);

    const confirmDelivered = async (order) => {
        try {
            setConfirmingDeliveryId(order.id);
            await changeOrderStatus(order.id, OrderStatus.Delivered);
            toast.success("Delivery confirmed — enjoy your meal");
            // Polling will move the order out of active and into history.
        } catch (err) {
            console.error("Failed to confirm delivery", err);
            if (!err?.toastShown) {
                toast.error(err.message ?? "Failed to confirm delivery");
            }
        } finally {
            setConfirmingDeliveryId(null);
        }
    };

    const handleRequestPay = async (order) => {
        if (payingOrderId) return;

        try {
            setPayingOrderId(order.id);

            // First check the payment status — if it's already Succeeded we
            // shouldn't show the modal at all (race with webhook arrival,
            // or stale UI before the next reloadOrders tick).
            const payment = await getPaymentByOrderId(order.id);
            if (payment?.status === "Succeeded") {
                toast.success("This order is already paid");
                reloadOrders?.({ silent: true });
                return;
            }

            const clientSecret = await getClientSecret(order.id);
            setPaymentForOrder({ order, clientSecret });
        } catch (err) {
            console.error("Failed to resume payment", err);
            if (!err?.toastShown) {
                toast.error(
                    err.message?.includes("Client secret not ready")
                        ? "Payment is still being prepared. Try again in a few seconds."
                        : err.message ?? "Could not open payment"
                );
            }
        } finally {
            setPayingOrderId(null);
        }
    };

    const handlePaymentSuccess = () => {
        const orderId = paymentForOrder?.order?.id;
        setPaymentForOrder(null);
        toast.success("Payment confirmed");
        // Webhook arrival → DB update → next reloadOrders tick will reflect
        // the paid state. Trigger an immediate silent refresh so the user
        // sees the change without waiting for the polling interval.
        reloadOrders?.({ silent: true });
        // (orderId currently unused beyond logging hooks; kept for clarity)
        void orderId;
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
            toast.success("Order cancelled");
        } catch (cancelError) {
            if (!cancelError?.toastShown) {
                toast.error(cancelError.message ?? "Failed to cancel order");
            }
        }
    };

    return (
        <div className="app-wrapper">
            <RoleSidebar />

            <main className="auth-homepage customer-orders-page">
                <CustomerOrdersHeader
                    filter={filter}
                    setFilter={setFilter}
                    sort={sort}
                    setSort={setSort}
                    search={search}
                    setSearch={setSearch}
                    count={filteredOrders.length}
                />

                <CustomerOrdersContent
                    loading={loading}
                    error={error}
                    orders={filteredOrders}
                    getStatusMeta={getStatusMeta}
                    onOpenDetails={openOrderDetails}
                    onTrackOrder={setTrackingOrder}
                    onRequestCancel={setOrderToCancel}
                    onConfirmDelivered={confirmDelivered}
                    onRequestPay={handleRequestPay}
                    confirmingDeliveryId={confirmingDeliveryId}
                    cancellingOrderId={cancellingOrderId}
                    payingOrderId={payingOrderId}
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

            {paymentForOrder && (
                <StripePaymentModal
                    open
                    onClose={() => setPaymentForOrder(null)}
                    clientSecret={paymentForOrder.clientSecret}
                    title={`Payment for «${paymentForOrder.order.restaurant}»`}
                    subtitle="Resume the payment that was started earlier. 3D Secure verification may be required."
                    onPaid={handlePaymentSuccess}
                />
            )}
        </div>
    );
};

export default CustomerOrdersPage;
