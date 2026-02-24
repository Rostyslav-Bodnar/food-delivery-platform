import React from "react";
import ActiveOrderCard from "./ActiveOrderCard";

export default function ActiveOrdersList({
                                             orders,
                                             getStatusMeta,
                                             onOpenDetails
                                         }) {
    return (
        <div className="active-orders-list">
            {orders.map(order => (
                <ActiveOrderCard
                    key={order.id}
                    order={order}
                    statusMeta={getStatusMeta(order.status)}
                    onOpenDetails={onOpenDetails}
                />
            ))}
        </div>
    );
}