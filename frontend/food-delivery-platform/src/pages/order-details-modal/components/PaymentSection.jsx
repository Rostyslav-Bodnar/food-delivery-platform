import React from "react";
import { Receipt, CreditCard, Clock } from "lucide-react";

export default function PaymentSection({ order }) {
    return (
        <section className="od-section">
            <h4><CreditCard size={16} /> Payment</h4>

            <div className="od-card">
                <p><Receipt size={16} /> <strong>Status:</strong> {order.paymentStatus}</p>
                <p><CreditCard size={16} /> <strong>Method:</strong> {order.paymentMethod}</p>
                <p className="muted">Transaction: {order.transactionId}</p>
                <p className="muted">
                    <Clock size={14} /> Payment time: {order.paymentDate}
                </p>
            </div>
        </section>
    );
}