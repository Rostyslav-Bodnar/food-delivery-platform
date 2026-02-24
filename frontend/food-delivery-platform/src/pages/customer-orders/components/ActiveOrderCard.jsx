import React from "react";

export default function ActiveOrderCard({
                                            order,
                                            statusMeta,
                                            onOpenDetails
                                        }) {
    return (
        <div className="active-order-card">
            <div className="active-order-header">
                <div className="order-id">
                    #{order.id.slice(0, 8)}
                </div>

                <div
                    className="live-status"
                    style={{ color: statusMeta.color }}
                >
                    {statusMeta.icon} {statusMeta.text}
                </div>
            </div>

            <div className="active-order-body">
                <h3>{order.restaurant}</h3>

                <div className="order-items-list">
                    {order.items.map((item, idx) => (
                        <div key={idx} className="order-item">
                            <span className="item-name">
                                {item.name}
                            </span>

                            <span className="item-details">
                                {item.quantity} × {item.price} ₴
                            </span>
                        </div>
                    ))}
                </div>

                {order.status === "on-the-way" && order.courier && (
                    <div className="courier-info">
                        <div className="courier-avatar">👤</div>
                        <div className="courier-name">
                            {order.courier.name}
                        </div>
                    </div>
                )}

                <div
                    style={{
                        padding: "20px 0 0",
                        borderTop: "1px solid var(--glass-border)",
                        marginTop: "20px"
                    }}
                >
                    <div
                        style={{
                            display: "flex",
                            justifyContent: "flex-end",
                            fontSize: "20px",
                            fontWeight: "700"
                        }}
                    >
                        Total: {order.total} ₴
                    </div>
                </div>
            </div>

            <div className="active-order-footer">
                <button className="track-btn">
                    Track on Map
                </button>

                <button
                    className="details-btn"
                    onClick={() => onOpenDetails(order)}
                >
                    Order Details
                </button>
            </div>
        </div>
    );
}
