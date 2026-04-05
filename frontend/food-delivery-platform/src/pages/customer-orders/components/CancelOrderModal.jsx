import React from "react";
import { AlertTriangle, LoaderCircle, X } from "lucide-react";

export default function CancelOrderModal({
    order,
    cancelling,
    onClose,
    onConfirm
}) {
    if (!order) {
        return null;
    }

    return (
        <div className="customer-orders-modal-overlay" onClick={onClose}>
            <div className="customer-orders-modal" onClick={(event) => event.stopPropagation()}>
                <button
                    type="button"
                    className="customer-orders-modal__close"
                    onClick={onClose}
                    aria-label="Close cancellation dialog"
                >
                    <X size={18} />
                </button>

                <div className="customer-orders-modal__icon is-danger">
                    <AlertTriangle size={22} />
                </div>

                <h3>Cancel this order?</h3>
                <p>
                    Order #{order.id.slice(0, 8)} from <strong>{order.restaurant}</strong> will be cancelled.
                    This action cannot be undone from the app.
                </p>

                <div className="customer-orders-modal__actions">
                    <button type="button" className="customer-orders-modal__ghost" onClick={onClose}>
                        Keep order
                    </button>
                    <button
                        type="button"
                        className="customer-orders-modal__danger"
                        onClick={onConfirm}
                        disabled={cancelling}
                    >
                        {cancelling ? <LoaderCircle size={16} className="spin" /> : null}
                        {cancelling ? "Cancelling..." : "Cancel order"}
                    </button>
                </div>
            </div>
        </div>
    );
}
