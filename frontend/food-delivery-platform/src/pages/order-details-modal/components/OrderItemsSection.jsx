import React from "react";
import { Package } from "lucide-react";

export default function OrderItemsSection({ items }) {
    return (
        <section className="od-section">
            <h4><Package size={16} /> Order</h4>

            <div className="od-items">
                {items.map((item, i) => (
                    <div key={i} className="od-item">
                        <span>{item.quantity}× {item.name}</span>
                        <span>{item.quantity * item.price} ₴</span>
                    </div>
                ))}
            </div>
        </section>
    );
}