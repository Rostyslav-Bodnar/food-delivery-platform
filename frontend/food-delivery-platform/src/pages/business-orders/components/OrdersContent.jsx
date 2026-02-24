import React from "react";
import { Package } from "lucide-react";
import OrdersGrid from "./OrdersGrid";

export default function OrdersContent({
                                          loading,
                                          filteredOrders,
                                          statusMap,
                                          onStatusChange,
                                          onOpenDetails
                                      }) {
    if (loading) {
        return <div className="bh-empty">Loading...</div>;
    }

    if (filteredOrders.length === 0) {
        return (
            <div className="bh-empty">
                <Package size={64} />
                <p>No orders</p>
            </div>
        );
    }

    return (
        <OrdersGrid
            orders={filteredOrders}
            statusMap={statusMap}
            onStatusChange={onStatusChange}
            onOpenDetails={onOpenDetails}
        />
    );
}