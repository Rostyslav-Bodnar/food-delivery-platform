import React from "react";
import ActiveOrderCard from "./ActiveOrderCard";

export default function ActiveOrdersList({
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
    return (
        <div className="active-orders-list">
            {orders.map((order) => (
                <ActiveOrderCard
                    key={order.id}
                    order={order}
                    statusMeta={getStatusMeta(order.status, order.deliveryMethod)}
                    onOpenDetails={onOpenDetails}
                    onTrackOrder={onTrackOrder}
                    onRequestCancel={onRequestCancel}
                    onConfirmDelivered={onConfirmDelivered}
                    onRequestPay={onRequestPay}
                    confirmingDelivery={confirmingDeliveryId === order.id}
                    cancelling={cancellingOrderId === order.id}
                    payingNow={payingOrderId === order.id}
                />
            ))}
        </div>
    );
}
