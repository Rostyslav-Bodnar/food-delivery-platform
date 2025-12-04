import React, { useState } from "react";
import { Package, Clock, CheckCircle, XCircle, ChevronRight } from "lucide-react";
import "./styles/BusinessOrdersPage.css";
import BusinessSidebar from "../components/business/BusinessSidebar.jsx";   

const STATUS_MAP = {
    pending: { label: "Нове", color: "#7c5cff", icon: Package },
    preparing: { label: "Готується", color: "#ffb86b", icon: Clock },
    ready: { label: "Готове", color: "#00d4ff", icon: CheckCircle },
    delivered: { label: "Доставлено", color: "#50fa7b", icon: CheckCircle },
    cancelled: { label: "Скасовано", color: "#ff6b6b", icon: XCircle },
};

// Мок-замовлення (як ніби прийшли з бекенду)
const MOCK_ORDERS = [
    {
        id: "1488",
        createdAt: "14:32",
        customerName: "Олександр П.",
        customerPhone: "+380 97 123 45 67",
        address: "вул. Франка 12, кв. 5",
        total: 1240,
        status: "pending",
        items: [
            { name: "Філадельфія класична", quantity: 2, price: 340 },
            { name: "Каліфорнія з лососем", quantity: 1, price: 360 },
            { name: "Місо-суп", quantity: 1, price: 120 },
        ],
    },
    {
        id: "1485",
        createdAt: "13:55",
        customerName: "Марія К.",
        customerPhone: "+380 63 987 65 43",
        address: "пр. Свободи 78",
        total: 680,
        status: "preparing",
        items: [
            { name: "Чізбургер меню", quantity: 1, price: 420 },
            { name: "Картопля фрі велика", quantity: 1, price: 140 },
            { name: "Кола 0.5л", quantity: 1, price: 60 },
        ],
    },
    {
        id: "1482",
        createdAt: "13:20",
        customerName: "Дмитро С.",
        customerPhone: "+380 50 111 22 33",
        address: "вул. Грушевського 5",
        total: 890,
        status: "ready",
        items: [
            { name: "Піца Маргарита 30см", quantity: 1, price: 520 },
            { name: "Цезар з куркою", quantity: 1, price: 370 },
        ],
    },
    {
        id: "1479",
        createdAt: "12:45",
        customerName: "Аліна В.",
        customerPhone: "+380 98 555 44 33",
        address: "вул. Шевченка 23",
        total: 450,
        status: "delivered",
        items: [
            { name: "Рол Філадельфія", quantity: 1, price: 340 },
            { name: "Соус васабі", quantity: 1, price: 30 },
        ],
    },
];

export default function BusinessOrdersPage({ userData }) {
    const [orders, setOrders] = useState(MOCK_ORDERS);
    const [filter, setFilter] = useState("all");

    const filteredOrders = filter === "all"
        ? orders
        : orders.filter(o => o.status === filter);

    const handleStatusChange = (orderId, newStatus) => {
        setOrders(prev =>
            prev.map(o => o.id === orderId ? { ...o, status: newStatus } : o)
        );
    };

    const businessName = userData?.currentAccount?.name || "Мій заклад";
    const businessLogo = userData?.currentAccount?.imageUrl;

    return (
        <div className="bh-page">
            <BusinessSidebar userData={userData} />

            <main className="bh-main">
                <header className="bh-top">
                    <h1 className="bh-heading">Замовлення</h1>
                    <div className="bh-controls">
                        <div className="filters">
                            <select value={filter} onChange={e => setFilter(e.target.value)}>
                                <option value="all">Усі</option>
                                <option value="pending">Нові</option>
                                <option value="preparing">Готується</option>
                                <option value="ready">Готові</option>
                                <option value="delivered">Доставлені</option>
                                <option value="cancelled">Скасовані</option>
                            </select>
                        </div>
                    </div>
                </header>

                <section className="bh-content">
                    {filteredOrders.length === 0 ? (
                        <div className="bh-empty">
                            <Package size={64} strokeWidth={1} />
                            <p>Немає замовлень</p>
                        </div>
                    ) : (
                        <div className="orders-grid">
                            {filteredOrders.map(order => {
                                const s = STATUS_MAP[order.status];
                                const StatusIcon = s.icon;

                                return (
                                    <div key={order.id} className="order-card">
                                        <div className="order-header">
                                            <div className="order-id">#{order.id}</div>
                                            <div className="order-time">{order.createdAt}</div>
                                        </div>

                                        <div className="order-body">
                                            <div className="customer-info">
                                                <strong>{order.customerName}</strong>
                                                <div>{order.customerPhone}</div>
                                                <div className="address">{order.address}</div>
                                            </div>

                                            <div className="order-items">
                                                {order.items.map((item, i) => (
                                                    <div key={i} className="item-row">
                                                        <span>{item.quantity}× {item.name}</span>
                                                        <span>{item.price * item.quantity} ₴</span>
                                                    </div>
                                                ))}
                                            </div>

                                            <div className="order-total">
                                                <strong>Разом: {order.total} ₴</strong>
                                            </div>
                                        </div>

                                        <div className="order-footer">
                                            <div className="status-badge" style={{ background: s.color + "22", color: s.color }}>
                                                <StatusIcon size={16} />
                                                {s.label}
                                            </div>

                                            <div className="status-actions">
                                                {order.status === "pending" && (
                                                    <>
                                                        <button onClick={() => handleStatusChange(order.id, "preparing")}>
                                                            Прийняти
                                                        </button>
                                                        <button className="danger" onClick={() => handleStatusChange(order.id, "cancelled")}>
                                                            Скасувати
                                                        </button>
                                                    </>
                                                )}
                                                {order.status === "preparing" && (
                                                    <button onClick={() => handleStatusChange(order.id, "ready")}>
                                                        Готово
                                                    </button>
                                                )}
                                                {order.status === "ready" && (
                                                    <button onClick={() => handleStatusChange(order.id, "delivered")}>
                                                        Видано
                                                    </button>
                                                )}
                                            </div>

                                            <button className="details-btn">
                                                Детальніше <ChevronRight size={16} />
                                            </button>
                                        </div>
                                    </div>
                                );
                            })}
                        </div>
                    )}
                </section>
            </main>
        </div>
    );
}