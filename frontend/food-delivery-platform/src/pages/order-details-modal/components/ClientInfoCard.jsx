import React from "react";
import { User, MapPin } from "lucide-react";

export default function ClientInfoCard({ order }) {
    return (
        <section className="od-section">
            <div className="od-card">
                <h4><User size={16} /> Customer</h4>
                <p><strong>{order.customerName}</strong></p>
                <p className="muted">{order.customerPhone}</p>
                <p className="muted">
                    <MapPin size={14} /> {order.address}
                </p>
            </div>
        </section>
    );
}