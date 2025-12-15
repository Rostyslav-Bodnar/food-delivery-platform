import React, { useState, useEffect } from 'react';
import { Link } from 'react-router-dom';
import './styles/CustomerOrdersPage.css';
import CustomerSidebar from '../components/customer-components/CustomerSidebar';
import OrderDetailsComponent from '../components/OrderDetailsComponent.jsx';

const CustomerOrdersPage = () => {
    const [orders, setOrders] = useState([]);
    const [loading, setLoading] = useState(true);
    const [selectedOrder, setSelectedOrder] = useState(null);

    useEffect(() => {
        setTimeout(() => {
            setOrders([
                {
                    id: '1328',
                    restaurant: 'Sushi Master',
                    status: 'preparing',
                    total: 980,
                    itemsCount: 4,
                    address: 'вул. Шевченка, 12, кв. 45',
                    courier: { name: 'Олег', phone: '+380501234567', rating: 4.9 },
                    items: [
                        { name: 'Філадельфія рол', quantity: 2, price: 220 },
                        { name: 'Суп місо', quantity: 1, price: 120 },
                        { name: 'Салат чука', quantity: 1, price: 90 },
                    ],
                    paymentStatus: 'Оплачено',
                    paymentMethod: 'Картка Visa',
                    transactionId: 'TXN-1328-2025',
                    paymentDate: '14.12.2025 17:30',
                    deliveryMethod: 'Курʼєрська доставка',
                    notes: 'Додати імбир та васабі',
                    createdAt: '14.12.2025 17:00',
                },
                {
                    id: '1325',
                    restaurant: 'Burger Lab',
                    status: 'on-the-way',
                    total: 520,
                    itemsCount: 3,
                    address: 'пр. Свободи, 78',
                    courier: { name: 'Аліна', phone: '+380671234567', rating: 5.0 },
                    items: [
                        { name: 'Бургер BBQ', quantity: 1, price: 250 },
                        { name: 'Картопля фрі', quantity: 1, price: 70 },
                        { name: 'Кола 0.5л', quantity: 1, price: 50 },
                    ],
                    paymentStatus: 'Очікує оплати',
                    paymentMethod: 'Готівка',
                    transactionId: 'TXN-1325-2025',
                    paymentDate: '-',
                    deliveryMethod: 'Курʼєрська доставка',
                    notes: 'Без майонезу',
                    createdAt: '14.12.2025 16:45',
                },
            ]);
            setLoading(false);
        }, 800);
    }, []);

    const getStatus = (status) => {
        if (status === 'preparing') return { text: 'Готується', color: '#ffb86b', icon: '🍳' };
        if (status === 'on-the-way') return { text: 'В дорозі', color: '#00d4ff', icon: '🏍️' };
        return { text: 'Нове', color: '#7c5cff', icon: '📦' };
    };

    return (
        <div className="app-wrapper">
            <CustomerSidebar />
            <main className="auth-homepage customer-orders-page">
                <div className="page-header">
                    <h1 className="gradient-title">Активні замовлення</h1>
                </div>
                {loading ? (
                    <div className="active-orders-list">
                        <div className="active-order-card skeleton"></div>
                        <div className="active-order-card skeleton"></div>
                    </div>
                ) : orders.length === 0 ? (
                    <div className="no-active-orders">
                        <div className="big-icon">🍕</div>
                        <h3>Немає активних замовлень</h3>
                        <p>Коли ви замовите їжу — статус з’явиться тут</p>
                        <Link to="/" className="big-cta-btn">Замовити зараз</Link>
                    </div>
                ) : (
                    <div className="active-orders-list">
                        {orders.map(order => {
                            const s = getStatus(order.status);
                            return (
                                <div key={order.id} className="active-order-card">
                                    <div className="active-order-header">
                                        <div className="order-id">#{order.id}</div>
                                        <div className="live-status" style={{ color: s.color }}>
                                            {s.icon} {s.text}
                                        </div>
                                    </div>
                                    <div className="active-order-body">
                                        <h3>{order.restaurant}</h3>

                                        {/* Прибрано рядок з кількістю позицій та сумою */}

                                        {/* Список замовлених страв */}
                                        <div className="order-items-list">
                                            {order.items.map((item, index) => (
                                                <div key={index} className="order-item">
                                                    <span className="item-name">{item.name}</span>
                                                    <span className="item-details">
                                                        {item.quantity} × {item.price} ₴
                                                    </span>
                                                </div>
                                            ))}
                                        </div>

                                        {/* Інформація про кур'єра (тільки для "В дорозі") */}
                                        {order.status === 'on-the-way' && (
                                            <div className="courier-info">
                                                <div className="courier-avatar">👤</div>
                                                <div>
                                                    <div className="courier-name">{order.courier.name}</div>
                                                    <div className="courier-rating">⭐ {order.courier.rating}</div>
                                                </div>
                                            </div>
                                        )}

                                        {/* Новий блок із загальною сумою перед футером */}
                                        <div style={{
                                            padding: '20px 0 0',
                                            borderTop: '1px solid var(--glass-border)',
                                            marginTop: '20px'
                                        }}>
                                            <div style={{
                                                display: 'flex',
                                                justifyContent: 'flex-end',
                                                alignItems: 'center',
                                                fontSize: '20px',
                                                fontWeight: '700',
                                                color: 'var(--text)'
                                            }}>
                                                До сплати: {order.total} ₴
                                            </div>
                                        </div>
                                    </div>

                                    <div className="active-order-footer">
                                        <button className="track-btn">
                                            Відстежити на карті
                                        </button>
                                        <button className="details-btn" onClick={() => setSelectedOrder(order)}>
                                            Деталі замовлення
                                        </button>
                                    </div>
                                </div>
                            );
                        })}
                    </div>
                )}
            </main>
            {selectedOrder && (
                <OrderDetailsComponent
                    order={selectedOrder}
                    statusMap={{
                        preparing: { label: 'Готується', color: '#ffb86b', icon: () => <span>🍳</span> },
                        'on-the-way': { label: 'В дорозі', color: '#00d4ff', icon: () => <span>🏍️</span> },
                        new: { label: 'Нове', color: '#7c5cff', icon: () => <span>📦</span> },
                    }}
                    onClose={() => setSelectedOrder(null)}
                />
            )}
        </div>
    );
};

export default CustomerOrdersPage;