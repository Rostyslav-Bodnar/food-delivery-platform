import React from "react";
import { Truck } from "lucide-react";

export default function CourierInfoCard({ courier }) {
    return (
        <section className="od-section">
            <div className="od-card">
                <h4><Truck size={16} /> Courier</h4>

                {courier ? (
                    <div className="od-courier">
                        <div className="od-courier-avatar">
                            {courier.name[0]}
                        </div>

                        <div>
                            <p><strong>{courier.name}</strong></p>
                            <p className="muted">{courier.phone}</p>
                        </div>
                    </div>
                ) : (
                    <p className="muted">Not Assigned</p>
                )}
            </div>
        </section>
    );
}
