import React from "react";
import { Truck, Clock } from "lucide-react";

export default function DeliverySection({ order }) {
    return (
        <section className="od-section">
            <h4><Truck size={16} /> Delivery</h4>

            <div className="od-card">
                <p><strong>Method:</strong> {order.deliveryMethod}</p>
                <p className="muted">
                    <Clock size={14} /> Estimated time: {order.eta}
                </p>
            </div>
        </section>
    );
}
