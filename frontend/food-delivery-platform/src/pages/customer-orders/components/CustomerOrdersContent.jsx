import React from "react";
import OrdersSkeleton from "./OrdersSkeleton";
import NoActiveOrders from "./NoActiveOrders";
import ActiveOrdersList from "./ActiveOrdersList";

export default function CustomerOrdersContent({
    loading,
    error,
    orders,
    getStatusMeta,
    onOpenDetails,
    onTrackOrder,
    onRequestCancel,
    onConfirmDelivered,
    onRequestPay,
    confirmingDeliveryId,
    cancellingOrderId,
    payingOrderId
}) {
    if (loading) {
        return <OrdersSkeleton />;
    }

    if (error) {
        return (
            <div className="orders-feedback-card">
                <h3>Could not load orders</h3>
                <p>{error}</p>
            </div>
        );
    }

    if (orders.length === 0) {
        return <NoActiveOrders />;
    }

    return (
        <ActiveOrdersList
            orders={orders}
            getStatusMeta={getStatusMeta}
            onOpenDetails={onOpenDetails}
            onTrackOrder={onTrackOrder}
            onRequestCancel={onRequestCancel}
            onConfirmDelivered={onConfirmDelivered}
            onRequestPay={onRequestPay}
            confirmingDeliveryId={confirmingDeliveryId}
            cancellingOrderId={cancellingOrderId}
            payingOrderId={payingOrderId}
        />
    );
}
