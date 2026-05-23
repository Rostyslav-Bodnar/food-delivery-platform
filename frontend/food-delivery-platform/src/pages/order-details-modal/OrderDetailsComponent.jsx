import React, { useEffect, useState } from "react";
import {
    Bike,
    CheckCircle2,
    Clock3,
    Package,
    XCircle
} from "lucide-react";
import "./styles/OrderDetailsComponent.css";

import OrderDetailsHeader from "./components/OrderDetailsHeader";
import OrderStatusBanner from "./components/OrderStatusBanner";
import ClientInfoCard from "./components/ClientInfoCard";
import CourierInfoCard from "./components/CourierInfoCard";
import OrderItemsSection from "./components/OrderItemsSection";
import PaymentSection from "./components/PaymentSection";
import DeliverySection from "./components/DeliverySection";
import OrderDetailsFooter from "./components/OrderDetailsFooter";

import { getOrderDetails } from "../../api/Order.ts";
import { getPaymentByOrderId } from "../../api/Payment.jsx";

// Keyed by the backend's OrderStatus enum names (case-insensitive lookup below).
const STATUS_META = {
    preparing: { label: "Preparing", color: "#ffb86b", icon: Clock3 },
    ready: { label: "Ready", color: "#00d4ff", icon: CheckCircle2 },
    outfordelivery: { label: "On the way", color: "#00d4ff", icon: Bike },
    pickedup: { label: "Picked up", color: "#50fa7b", icon: Bike },
    delivered: { label: "Delivered", color: "#50fa7b", icon: CheckCircle2 },
    canceled: { label: "Cancelled", color: "#ff6b6b", icon: XCircle }
};

const FALLBACK_STATUS = { label: "Unknown", color: "#8a8a8a", icon: Package };

const lookupStatusMeta = (status) => {
    const key = String(status ?? "").toLowerCase().replace(/[^a-z]/g, "");
    return STATUS_META[key] ?? FALLBACK_STATUS;
};

export default function OrderDetailsComponent({ orderId, onClose }) {
    const [order, setOrder] = useState(null);
    const [payment, setPayment] = useState(null);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState(null);

    useEffect(() => {
        if (!orderId) return undefined;

        let cancelled = false;
        setLoading(true);
        setError(null);

        (async () => {
            try {
                const details = await getOrderDetails(orderId);
                if (cancelled) return;
                setOrder(details);
            } catch (err) {
                if (!cancelled) {
                    setError(err.message || "Failed to load order details");
                }
            }

            // Payment may legitimately not exist yet (just-placed orders, or
            // cash-on-delivery before the consumer wrote the row). Tolerate.
            try {
                const paymentDto = await getPaymentByOrderId(orderId);
                if (!cancelled) setPayment(paymentDto);
            } catch {
                if (!cancelled) setPayment(null);
            }

            if (!cancelled) setLoading(false);
        })();

        return () => { cancelled = true; };
    }, [orderId]);

    if (!orderId) return null;

    return (
        <div className="od-overlay" onClick={onClose}>
            <div className="od-modal wide" onClick={(e) => e.stopPropagation()}>
                {loading && !order && (
                    <div style={{ padding: "32px", textAlign: "center", color: "#aaa" }}>
                        Loading order details…
                    </div>
                )}

                {error && !order && (
                    <div style={{ padding: "32px", textAlign: "center", color: "#ff6b6b" }}>
                        {error}
                    </div>
                )}

                {order && (
                    <>
                        <OrderDetailsHeader order={order} onClose={onClose} />

                        <OrderStatusBanner status={lookupStatusMeta(order.orderStatus)} />

                        <div className="od-grid">
                            <div className="od-col">
                                <ClientInfoCard order={order} />
                                <CourierInfoCard order={order} />
                                <DeliverySection order={order} />
                            </div>

                            <div className="od-col">
                                <OrderItemsSection items={order.dishes} />
                                <PaymentSection order={order} payment={payment} />
                            </div>
                        </div>

                        <OrderDetailsFooter
                            total={(order.totalPrice ?? 0) + (order.deliveryFee ?? 0)}
                        />
                    </>
                )}
            </div>
        </div>
    );
}
