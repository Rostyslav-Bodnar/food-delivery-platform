import React from "react";
import { Clock3, MapPin, Wallet } from "lucide-react";

export default function ActiveOrderCard({
    order,
    statusMeta,
    onOpenDetails,
    onTrackOrder,
    onRequestCancel,
    cancelling
}) {
    
    console.log("ActiveOrderCard", order);
    
    return (
        <div className="active-order-card">
            <div className="active-order-header">
                <div className="order-id">#{order.id.slice(0, 8)}</div>

                <div className="live-status" style={{ color: statusMeta.color }}>
                    {statusMeta.icon} {statusMeta.text}
                </div>
            </div>

            <div className="active-order-body">
                <h3>{order.restaurant}</h3>

                <div className="order-meta-stack">
                    <div className="order-meta-pill">
                        <Clock3 size={14} />
                        {order.createdAt}
                    </div>
                    <div className="order-meta-pill">
                        <MapPin size={14} />
                        {order.address}
                    </div>
                    <div className="order-meta-pill">
                        <Wallet size={14} />
                        {order.total} ₴ total
                    </div>
                </div>

                <div className="order-items-list">
                    {order.items.map((item) => (
                        <div key={item.id} className="order-item">
                            <span className="item-name">{item.name}</span>
                            <span className="item-details">
                                {item.quantity} × {item.price} ₴
                            </span>
                        </div>
                    ))}
                </div>

                {order.status === "on-the-way" && order.courier && (
                    <div className="courier-info">
                        <div className="courier-avatar">👤</div>
                        <div className="courier-name">{order.courier.name}</div>
                    </div>
                )}

                <div className="order-total">Total: {order.total} ₴</div>
            </div>

            <div className="active-order-footer">
                <button className="track-btn" onClick={() => onTrackOrder(order)}>
                    Track on Map
                </button>

                <button className="details-btn" onClick={() => onOpenDetails(order)}>
                    Order Details
                </button>

                {order.canCancel && (
                    <button
                        className="cancel-btn"
                        onClick={() => onRequestCancel(order)}
                        disabled={cancelling}
                    >
                        {cancelling ? "Cancelling..." : "Cancel"}
                    </button>
                )}
            </div>
        </div>
    );
}
