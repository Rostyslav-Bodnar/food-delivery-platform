import React from "react";
import OrderCard from "./OrderCard";

export default function OrdersGrid({
                                       orders,
                                       statusMap,
                                       onStatusChange,
                                       onOpenDetails
                                   }) {
    return (
        <div className="orders-grid">
            {orders.map(order => (
                <OrderCard
                    key={order.id}
                    order={order}
                    statusMap={statusMap}
                    onStatusChange={onStatusChange}
                    onOpenDetails={onOpenDetails}
                />
            ))}
        </div>
    );
}