import React from "react";
import { User, MapPin } from "lucide-react";

export default function ClientInfoCard({ order }) {
    const name = order.customerFullName?.trim() || "Customer";
    const phone = order.customerPhoneNumber || "";
    const address = order.customerAddress || "";

    return (
        <section className="od-section">
            <div className="od-card">
                <h4><User size={16} /> Customer</h4>
                <p><strong>{name}</strong></p>
                {phone && <p className="muted">{phone}</p>}
                {address && (
                    <p className="muted">
                        <MapPin size={14} /> {address}
                    </p>
                )}
            </div>
        </section>
    );
}
