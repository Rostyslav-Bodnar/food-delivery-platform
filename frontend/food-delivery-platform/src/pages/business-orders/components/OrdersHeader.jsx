import React from "react";

export default function OrdersHeader({ filter, setFilter }) {
    return (
        <header className="bh-top">
            <h1 className="bh-heading">Orders</h1>

            <div className="filters">
                <select value={filter} onChange={e => setFilter(e.target.value)}>
                    <option value="all">All</option>
                    <option value="pending">New</option>
                    <option value="preparing">Preparing</option>
                    <option value="ready">Ready</option>
                    <option value="delivered">Delivered</option>
                    <option value="cancelled">Cancelled</option>
                </select>
            </div>
        </header>
    );
}