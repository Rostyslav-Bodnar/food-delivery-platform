import React from "react";
import { X, Receipt, CreditCard, Truck, FileText, User, Package, MapPin, Clock } from "lucide-react";
import "./styles/OrderDetailsComponent.css";

export default function OrderDetailsComponent({ order, statusMap, onClose }) {
    if (!order) return null;
    const s = statusMap[order.status];
    const StatusIcon = s.icon;
    // мок дані для демо
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
                {/* HEADER */}
                <header className="od-header">
                    <div>
                        <h2>Замовлення #{mockOrder.id}</h2>
                        <span className="od-time">{mockOrder.createdAt}</span>
                    </div>
                    <button className="od-close" onClick={onClose}>
                        <X size={18} />
                    </button>
                </header>
                {/* STATUS */}
                <div
                    className="od-status"
                    style={{ background: `linear-gradient(90deg, ${s.color}22, ${s.color}11)`, color: s.color }}
                >
                    <StatusIcon size={18} />
                    {s.label}
                </div>
                {/* MAIN GRID */}
                <div className="od-grid">
                    {/* LEFT COLUMN: PEOPLE & LOCATIONS */}
                    <div className="od-col">
                        {/* CLIENT */}
                        <section className="od-section">
                            <div className="od-card">
                                <h4><User size={16} /> Клієнт</h4>
                                <p><strong>{mockOrder.customerName}</strong></p>
                                <p className="muted">{mockOrder.customerPhone}</p>
                                <p className="muted"><MapPin size={14} /> {mockOrder.address}</p>
                            </div>
                        </section>
                        {/* COURIER */}
                        <section className="od-section">
                            <div className="od-card">
                                <h4><Truck size={16} /> Курʼєр</h4>
                                {mockOrder.courier ? (
                                    <div className="od-courier">
                                        <div className="od-courier-avatar">
                                            {mockOrder.courier.name[0]}
                                        </div>
                                        <div>
                                            <p><strong>{mockOrder.courier.name}</strong></p>
                                            <p className="muted">{mockOrder.courier.phone}</p>
                                        </div>
                                    </div>
                                ) : (
                                    <p className="muted">Не призначений</p>
                                )}
                            </div>
                        </section>
                        {/* NOTES */}
                        {mockOrder.notes && (
                            <section className="od-section">
                                <h4><FileText size={16} /> Примітки клієнта</h4>
                                <div className="od-card">
                                    <p>{mockOrder.notes}</p>
                                </div>
                            </section>
                        )}
                    </div>
                    {/* RIGHT COLUMN: ORDER & PAYMENT */}
                    <div className="od-col">
                        {/* ITEMS */}
                        <section className="od-section">
                            <h4><Package size={16} /> Замовлення</h4>
                            <div className="od-items">
                                {mockOrder.items.map((item, i) => (
                                    <div key={i} className="od-item">
                                        <span>{item.quantity}× {item.name}</span>
                                        <span>{item.quantity * item.price} ₴</span>
                                    </div>
                                ))}
                            </div>
                        </section>
                        {/* PAYMENT */}
                        <section className="od-section">
                            <h4><CreditCard size={16} /> Оплата</h4>
                            <div className="od-card">
                                <p><Receipt size={16} /> <strong>Статус:</strong> {mockOrder.paymentStatus}</p>
                                <p><CreditCard size={16} /> <strong>Метод:</strong> {mockOrder.paymentMethod}</p>
                                <p className="muted">Транзакція: {mockOrder.transactionId}</p>
                                <p className="muted"><Clock size={14} /> Час оплати: {mockOrder.paymentDate}</p>
                            </div>
                        </section>
                        {/* DELIVERY */}
                        <section className="od-section">
                            <h4><Truck size={16} /> Доставка</h4>
                            <div className="od-card">
                                <p><strong>Спосіб:</strong> {mockOrder.deliveryMethod}</p>
                                <p className="muted"><Clock size={14} /> Очікуваний час: {mockOrder.eta}</p>
                            </div>
                        </section>
                    </div>
                </div>
                {/* FOOTER */}
                <footer className="od-footer">
                    <span>Разом</span>
                    <strong>{mockOrder.total} ₴</strong>
                </footer>
            </div>
        </div>
    );
}