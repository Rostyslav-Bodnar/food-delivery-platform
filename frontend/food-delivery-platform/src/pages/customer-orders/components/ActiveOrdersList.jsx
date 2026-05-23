import React from "react";
import ActiveOrderCard from "./ActiveOrderCard";

export default function ActiveOrdersList({
    orders,
    getStatusMeta,
    onOpenDetails,
    onTrackOrder,
    onRequestCancel,
    onConfirmDelivered,
    confirmingDeliveryId,
    cancellingOrderId
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
                    confirmingDelivery={confirmingDeliveryId === order.id}
                    cancelling={cancellingOrderId === order.id}
                />
            ))}
        </div>
    );
}
