import React from "react";
import { X } from "lucide-react";

const formatOrderDate = (value) => {
    if (!value) return "";
    const d = new Date(value);
    if (Number.isNaN(d.getTime())) return "";
    return d.toLocaleString("uk-UA", {
        day: "2-digit",
        month: "2-digit",
        year: "numeric",
        hour: "2-digit",
        minute: "2-digit"
    });
};

const shortenId = (id) => (id ? String(id).slice(0, 8) : "");

export default function OrderDetailsHeader({ order, onClose }) {
    return (
        <header className="od-header">
            <div>
                <h2>Order #{shortenId(order.id)}</h2>
                <span className="od-time">{formatOrderDate(order.orderDate)}</span>
            </div>

            <button className="od-close" onClick={onClose}>
                <X size={18} />
            </button>
        </header>
    );
}
