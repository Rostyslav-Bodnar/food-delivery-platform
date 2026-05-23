import React from "react";
import { ChevronRight, Route } from "lucide-react";
import StatusBadge from "./StatusBadge";
import StatusActions from "./StatusActions";

const formatCurrency = (value) =>
    new Intl.NumberFormat("uk-UA", {
        style: "currency",
        currency: "UAH",
        maximumFractionDigits: 0
    }).format(Number(value ?? 0));

export default function OrderCard({
    order,
    statusMap,
    onStatusChange,
    onOpenDetails,
    onTrackOrder
}) {
    const statusDefinition = statusMap[order.status];
    const StatusIcon = statusDefinition.icon;

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
                    {order.items.map((item, index) => (
                        <div key={index} className="item-row">
                            <span>{item.quantity}x {item.name}</span>
                            <span>{formatCurrency(item.price * item.quantity)}</span>
                        </div>
                    ))}
                </div>

                <div className="order-total">
                    Subtotal: {formatCurrency(order.total)}
                </div>
            </div>

            <div className="order-footer">
                <StatusBadge status={statusDefinition} StatusIcon={StatusIcon} />

                <StatusActions
                    status={order.status}
                    orderId={order.id}
                    deliveryMethod={order.deliveryMethod}
                    onStatusChange={onStatusChange}
                />

                <div className="order-footer__actions">
                    <button
                        className="track-order-btn"
                        onClick={() => onTrackOrder(order)}
                    >
                        <Route size={15} />
                        Track live
                    </button>

                    <button
                        className="details-btn"
                        onClick={() => onOpenDetails(order)}
                    >
                        Details <ChevronRight size={16} />
                    </button>
                </div>
            </div>
        </div>
    );
}
