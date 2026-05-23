import React from "react";
import { Truck } from "lucide-react";

export default function CourierInfoCard({ order }) {
    const assigned = Boolean(order.deliveredById);
    const name = order.courierName?.trim();
    const phone = order.courierPhoneNumber;

    return (
        <section className="od-section">
            <div className="od-card">
                <h4><Truck size={16} /> Courier</h4>

                {assigned && name ? (
                    <div className="od-courier">
                        <div className="od-courier-avatar">{name[0]}</div>
                        <div>
                            <p><strong>{name}</strong></p>
                            {phone && <p className="muted">{phone}</p>}
                        </div>
                    </div>
                ) : (
                    <p className="muted">Not assigned</p>
                )}
            </div>
        </section>
    );
}
