import React from "react";
import OrdersSkeleton from "./OrdersSkeleton";
import NoActiveOrders from "./NoActiveOrders";
import ActiveOrdersList from "./ActiveOrdersList";

export default function CustomerOrdersContent({
                                                  loading,
                                                  orders,
                                                  getStatusMeta,
                                                  onOpenDetails
                                              }) {
    if (loading) return <OrdersSkeleton />;

    if (orders.length === 0) return <NoActiveOrders />;

    return (
        <ActiveOrdersList
            orders={orders}
            getStatusMeta={getStatusMeta}
            onOpenDetails={onOpenDetails}
        />
    );
}