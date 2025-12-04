import React, { useState, useEffect } from 'react';
import { Link } from 'react-router-dom';
import './styles/CustomerOrdersPage.css';
import CustomerSidebar from '../components/customer-components/CustomerSidebar';

const CustomerOrdersPage = () => {
    const [orders, setOrders] = useState([]);
    const [loading, setLoading] = useState(true);

    useEffect(() => {
        setTimeout(() => {
            setOrders([
                {
                    id: '1328',
                    restaurant: 'Sushi Master',
                    status: 'preparing',
                    progress: 70,
                    eta: '20–30 хв',
                    total: 980,
                    itemsCount: 4,
                    address: 'вул. Шевченка, 12, кв. 45',
                    courier: { name: 'Олег', rating: 4.9 },
                },
                {
                    id: '1325',
                    restaurant: 'Burger Lab',
                    status: 'on-the-way',
                    progress: 92,
                    eta: '5–10 хв',
                    total: 520,
                    itemsCount: 2,
                    address: 'пр. Свободи, 78',
                    courier: { name: 'Аліна', rating: 5.0 },
                },
            ]);
            setLoading(false);
        }, 800);
    }, []);

    const getStatus = (status) => {
        if (status === 'preparing') return { text: 'Готується', color: '#ffb86b', icon: 'CookingPot' };
        if (status === 'on-the-way') return { text: 'В дорозі', color: '#00d4ff', icon: 'Motorcycle' };
        return { text: 'Нове', color: '#7c5cff', icon: 'Package' };
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
                        <div className="big-icon">Pizza</div>
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
                                        <div className="order-details">
                                            <span>{order.itemsCount} позиції • {order.total} ₴</span>
                                            <span className="eta">Приблизно {order.eta}</span>
                                        </div>

                                        <div className="progress-container">
                                            <div className="progress-bar">
                                                <div
                                                    className="progress-fill"
                                                    style={{ width: `${order.progress}%`, background: s.color }}
                                                ></div>
                                            </div>
                                            <span className="progress-text">{order.progress}%</span>
                                        </div>

                                        {order.status === 'on-the-way' && (
                                            <div className="courier-info">
                                                <div className="courier-avatar">Person</div>
                                                <div>
                                                    <div className="courier-name">{order.courier.name}</div>
                                                    <div className="courier-rating">Rating {order.courier.rating}</div>
                                                </div>
                                            </div>
                                        )}
                                    </div>

                                    <div className="active-order-footer">
                                        <button className="track-btn">
                                            Відстежити на карті
                                        </button>
                                        <button className="details-btn">Деталі замовлення</button>
                                    </div>
                                </div>
                            );
                        })}
                    </div>
                )}
            </main>
        </div>
    );
};

export default CustomerOrdersPage;