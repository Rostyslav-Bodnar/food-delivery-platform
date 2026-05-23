import React from "react";
import { Receipt, CreditCard } from "lucide-react";

const METHOD_LABEL = {
    online: "Card (online)",
    cashondelivery: "Cash on delivery"
};

const STATUS_LABEL = {
    pending: "Pending",
    requiresaction: "Requires action",
    awaitingcashcollection: "Awaiting cash collection",
    succeeded: "Paid",
    failed: "Failed",
    cancelled: "Cancelled",
    refunded: "Refunded"
};

const normalize = (value) =>
    String(value ?? "").toLowerCase().replace(/[^a-z]/g, "");

const formatMoney = (amount, currency) => {
    const n = Number(amount ?? 0);
    const code = (currency ?? "").toUpperCase();
    return code ? `${n.toFixed(2)} ${code}` : n.toFixed(2);
};

export default function PaymentSection({ order, payment }) {
    const method = payment?.method ?? "";
    const status = payment?.status ?? "";

    const methodLabel = METHOD_LABEL[normalize(method)] ?? method ?? "—";
    const statusLabel = STATUS_LABEL[normalize(status)] ?? status ?? "—";

    return (
        <section className="od-section">
            <h4><CreditCard size={16} /> Payment</h4>

            <div className="od-card">
                {payment ? (
                    <>
                        <p><Receipt size={16} /> <strong>Status:</strong> {statusLabel}</p>
                        <p><CreditCard size={16} /> <strong>Method:</strong> {methodLabel}</p>
                        <p><strong>Amount:</strong> {formatMoney(payment.amount, payment.currency)}</p>
                        {Number(payment.totalRefunded ?? 0) > 0 && (
                            <p className="muted">
                                Refunded: {formatMoney(payment.totalRefunded, payment.currency)}
                            </p>
                        )}
                        {payment.stripePaymentIntentId && (
                            <p className="muted">Transaction: {payment.stripePaymentIntentId}</p>
                        )}
                    </>
                ) : (
                    <p className="muted">Payment record not available yet.</p>
                )}
                <p className="muted">Subtotal: {Number(order.totalPrice ?? 0).toFixed(2)} ₴</p>
                <p className="muted">Delivery fee: {Number(order.deliveryFee ?? 0).toFixed(2)} ₴</p>
            </div>
        </section>
    );
}
