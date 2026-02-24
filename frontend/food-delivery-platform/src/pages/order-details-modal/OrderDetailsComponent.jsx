import React from "react";
import "./styles/OrderDetailsComponent.css";

import OrderDetailsHeader from "./components/OrderDetailsHeader";
import OrderStatusBanner from "./components/OrderStatusBanner";
import ClientInfoCard from "./components/ClientInfoCard";
import CourierInfoCard from "./components/CourierInfoCard";
import NotesSection from "./components/NotesSection";
import OrderItemsSection from "./components/OrderItemsSection";
import PaymentSection from "./components/PaymentSection";
import DeliverySection from "./components/DeliverySection";
import OrderDetailsFooter from "./components/OrderDetailsFooter";

export default function OrderDetailsComponent({ order, statusMap, onClose }) {
    if (!order) return null;

    const s = statusMap[order.status];

    // мок дані
    const mockOrder = {
        ...order,
        paymentStatus: "Оплачено",
        paymentMethod: "Картка Visa",
        transactionId: "TXN-123456789",
        paymentDate: "2025-12-14 17:30",
        deliveryMethod: "Курʼєрська доставка",
        eta: "18:15",
        notes: "Будь ласка, без цибулі у піці 🍕",
    };

    return (
        <div className="od-overlay" onClick={onClose}>
            <div className="od-modal wide" onClick={e => e.stopPropagation()}>

                <OrderDetailsHeader order={mockOrder} onClose={onClose} />

                <OrderStatusBanner status={s} />

                <div className="od-grid">
                    <div className="od-col">
                        <ClientInfoCard order={mockOrder} />
                        <CourierInfoCard courier={mockOrder.courier} />
                        <NotesSection notes={mockOrder.notes} />
                    </div>

                    <div className="od-col">
                        <OrderItemsSection items={mockOrder.items} />
                        <PaymentSection order={mockOrder} />
                        <DeliverySection order={mockOrder} />
                    </div>
                </div>

                <OrderDetailsFooter total={mockOrder.total} />

            </div>
        </div>
    );
}