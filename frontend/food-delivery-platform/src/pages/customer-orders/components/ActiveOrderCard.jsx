import React from "react";
import { Bike, Clock3, CreditCard, MapPin, Wallet } from "lucide-react";

const formatCurrency = (value) =>
    new Intl.NumberFormat("uk-UA", {
        style: "currency",
        currency: "UAH",
        maximumFractionDigits: 0
    }).format(Number(value ?? 0));

export default function ActiveOrderCard({
    order,
    statusMeta,
    onOpenDetails,
    onTrackOrder,
    onRequestCancel,
    onConfirmDelivered,
    onRequestPay,
    confirmingDelivery,
    cancelling,
    payingNow
}) {
    const canConfirmDelivered =
        order.deliveryMethod !== "Pickup" && order.status === "picked-up";

    // Card-payment orders can be paid from this page if the original
    // checkout flow was abandoned (user closed the Stripe modal). The PI
    // stays open on Stripe's side, so the same clientSecret resumes the
    // unfinished payment. Hidden for cash, delivered, and cancelled orders.
    const canPayNow =
        order.paymentMethod === "Online" &&
        order.status !== "delivered" &&
        order.status !== "cancelled";

    return (
        <div className="active-order-card">
            <div className="active-order-header">
                <div className="order-id">#{order.id.slice(0, 8)}</div>

                <div className="live-status" style={{ color: statusMeta.color }}>
                    {statusMeta.icon}
                    {statusMeta.text}
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
                </div>

                <div className="order-items-list">
                    {order.items.map((item) => (
                        <div key={item.id} className="order-item">
                            <span className="item-name">{item.name}</span>
                            <span className="item-details">
                                {item.quantity} x {formatCurrency(item.price)}
                            </span>
                        </div>
                    ))}
                </div>

                {order.courier?.name && (
                    <div className="courier-info">
                        <div className="courier-avatar">
                            <Bike size={16} />
                        </div>
                        <div className="courier-name">{order.courier.name}</div>
                    </div>
                )}

                <div className="order-total">Total: {formatCurrency(order.total)}</div>
            </div>

            <div className="active-order-footer">
                {order.deliveryMethod !== "Pickup" && (
                    <button className="track-btn" onClick={() => onTrackOrder(order)}>
                        Track live
                    </button>
                )}

                <button className="details-btn" onClick={() => onOpenDetails(order)}>
                    Order details
                </button>

                {canConfirmDelivered && (
                    <button
                        className="track-btn"
                        onClick={() => onConfirmDelivered?.(order)}
                        disabled={confirmingDelivery}
                    >
                        {confirmingDelivery ? "Confirming..." : "Confirm received"}
                    </button>
                )}

                {canPayNow && (
                    <button
                        className="pay-now-btn"
                        onClick={() => onRequestPay?.(order)}
                        disabled={payingNow}
                    >
                        <CreditCard size={14} />
                        {payingNow ? "Opening payment..." : "Pay now"}
                    </button>
                )}

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
