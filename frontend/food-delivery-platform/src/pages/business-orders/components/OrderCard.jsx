import React from "react";
import { ChevronRight } from "lucide-react";
import StatusBadge from "./StatusBadge";
import StatusActions from "./StatusActions";

export default function OrderCard({
                                      order,
                                      statusMap,
                                      onStatusChange,
                                      onOpenDetails
                                  }) {
    const s = statusMap[order.status];
    const StatusIcon = s.icon;

    return (
        <div className="order-card">
            <div className="order-header">
                <div className="order-id">#{order.id.slice(0, 8)}</div>
                <div className="order-time">{order.createdAt}</div>
            </div>

            <div className="order-body">
                <strong>{order.customerName}</strong>
                <div className="address">{order.address}</div>

                <div className="order-items">
                    {order.items.map((item, i) => (
                        <div key={i} className="item-row">
                            <span>{item.quantity}× {item.name}</span>
                            <span>{item.price * item.quantity} ₴</span>
                        </div>
                    ))}
                </div>

                <div className="order-total">
                    Subtotal: {order.total} ₴
                </div>
            </div>

            <div className="order-footer">
                <StatusBadge status={s} StatusIcon={StatusIcon} />

                <StatusActions
                    status={order.status}
                    orderId={order.id}
                    onStatusChange={onStatusChange}
                />

                <button
                    className="details-btn"
                    onClick={() => onOpenDetails(order)}
                >
                    Details <ChevronRight size={16} />
                </button>
            </div>
        </div>
    );
}