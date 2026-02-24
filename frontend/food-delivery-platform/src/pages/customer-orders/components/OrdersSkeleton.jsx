import React from "react";

export default function OrdersSkeleton() {
    return (
        <div className="active-orders-list">
            <div className="active-order-card skeleton"></div>
            <div className="active-order-card skeleton"></div>
        </div>
    );
}