import React from "react";
import { Package } from "lucide-react";

export default function OrderItemsSection({ items = [] }) {
    return (
        <section className="od-section">
            <h4><Package size={16} /> Order</h4>

            <div className="od-items">
                {items.map((item) => {
                    const name = item.dishName ?? item.name ?? "";
                    const quantity = Number(item.quantity ?? 0);
                    const price = Number(item.price ?? 0);
                    return (
                        <div key={item.id} className="od-item">
                            <span>{quantity}× {name}</span>
                            <span>{(quantity * price).toFixed(2)} ₴</span>
                        </div>
                    );
                })}
            </div>
        </section>
    );
}
