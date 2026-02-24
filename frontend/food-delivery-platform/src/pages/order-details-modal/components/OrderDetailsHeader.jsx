import React from "react";
import { X } from "lucide-react";

export default function OrderDetailsHeader({ order, onClose }) {
    return (
        <header className="od-header">
            <div>
                <h2>Order #{order.id}</h2>
                <span className="od-time">{order.createdAt}</span>
            </div>

            <button className="od-close" onClick={onClose}>
                <X size={18} />
            </button>
        </header>
    );
}
