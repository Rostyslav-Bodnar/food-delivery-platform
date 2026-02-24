import React from "react";
import { Link } from "react-router-dom";

export default function NoActiveOrders() {
    return (
        <div className="no-active-orders">
            <div className="big-icon">🍕</div>
            <h3>No Active Orders</h3>
            <p>Once you place an order — its status will appear here</p>

            <Link to="/" className="big-cta-btn">
                Order Now
            </Link>
        </div>
    );
}
