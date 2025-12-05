// src/pages/courier/CourierHomePage.jsx
import React, { useState, useEffect } from 'react';
import { motion, AnimatePresence } from 'framer-motion';
import {
    Package, MapPin, Phone, Clock, CheckCircle, Bike,
    DollarSign, History, Power, Calendar, Star
} from 'lucide-react';
import './styles/CourierHomePage.css';

// Мок-дані з повними адресами та телефонами ресторанів
const mockNewOrders = [
    {
        id: 101,
        restaurant: "Pizza Palace",
        restaurantAddress: "вул. Хрещатик, 22",
        restaurantPhone: "+380 44 123 45 67",
        address: "вул. Антоновича 72, кв. 28",
        price: 389,
        items: 4,
        time: "15 хв",
        distance: "2.4 км"
    },
    {
        id: 102,
        restaurant: "Sushi Master",
        restaurantAddress: "пр. Перемоги, 45",
        restaurantPhone: "+380 98 777 88 99",
        address: "вул. Січових Стрільців 10, кв. 5",
        price: 629,
        items: 7,
        time: "22 хв",
        distance: "4.1 км"
    },
    {
        id: 103,
        restaurant: "Burger Hub",
        restaurantAddress: "вул. Саксаганського, 88",
        restaurantPhone: "+380 63 555 22 11",
        address: "бул. Лесі Українки 15",
        price: 219,
        items: 2,
        time: "10 хв",
        distance: "1.8 км"
    },
];

const mockActiveOrder = null; // Починаємо без активного

// Мок-історія (додамо сюди завершені замовлення)
const mockHistory = [
    { id: 98, restaurant: "KFC", clientName: "Максим П.", earned: 95, price: 380, date: "Сьогодні, 14:32", rating: 5 },
    { id: 97, restaurant: "McDonald's", clientName: "Аліна С.", earned: 110, price: 440, date: "Сьогодні, 12:15", rating: 4 },
    { id: 96, restaurant: "Sushi Master", clientName: "Дмитро К.", earned: 180, price: 720, date: "Вчора, 19:47", rating: 5 },
    { id: 95, restaurant: "Pizza Palace", clientName: "Софія Л.", earned: 135, price: 540, date: "Вчора, 17:20", rating: 5 },
];

export default function CourierHomePage({ userData }) {
    const [isOnline, setIsOnline] = useState(true);
    const [activeTab, setActiveTab] = useState("new");
    const [activeOrder, setActiveOrder] = useState(mockActiveOrder);
    const [newOrders, setNewOrders] = useState(mockNewOrders);
    const [history, setHistory] = useState(mockHistory);

    // Таймер
    useEffect(() => {
        if (!activeOrder) return;
        const interval = setInterval(() => {
            setActiveOrder(prev => {
                if (!prev) return prev;
                const [min, sec] = prev.timeLeft.split(':').map(Number);
                let total = min * 60 + sec - 1;
                if (total <= 0) {
                    clearInterval(interval);
                    return prev;
                }
                const newMin = String(Math.floor(total / 60)).padStart(2, '0');
                const newSec = String(total % 60).padStart(2, '0');
                return { ...prev, timeLeft: `${newMin}:${newSec}` };
            });
        }, 1000);
        return () => clearInterval(interval);
    }, [activeOrder]);

    const acceptOrder = (order) => {
        setActiveOrder({
            ...order,
            clientAddress: order.address,
            clientName: "Олена К.",
            clientPhone: "+380 67 123 45 67",
            earned: Math.round(order.price * 0.25),
            timeLeft: "20:00",
            status: "waiting_pickup"
        });
        setNewOrders(prev => prev.filter(o => o.id !== order.id));
        setActiveTab("active");
    };

    const completeDelivery = () => {
        if (!activeOrder) return;

        // Додаємо в історію
        const completedOrder = {
            id: activeOrder.id,
            restaurant: activeOrder.restaurant,
            clientName: activeOrder.clientName,
            earned: activeOrder.earned,
            price: activeOrder.price,
            date: "Сьогодні, " + new Date().toLocaleTimeString('uk-UA', { hour: '2-digit', minute: '2-digit' }),
            rating: Math.floor(Math.random() * 2) + 4 // 4 або 5
        };

        setHistory(prev => [completedOrder, ...prev]);
        alert(`Доставку завершено! +${activeOrder.earned} ₴ на баланс`);
        setActiveOrder(null);
        setActiveTab("new");
    };

    return (
        <div className="app-wrapper">
            {/* САЙДБАР */}
            <aside className="courier-sidebar">
                <div className="sidebar-logo">ШвидкоДоставка</div>
                <nav className="sidebar-nav">
                    <a href="#" className={`sidebar-item ${activeTab === "new" ? "active" : ""}`}
                        onClick={() => setActiveTab("new")}>
                        <Package size={20} /> <span>Нові замовлення</span>
                    </a>
                    <a href="#" className={`sidebar-item ${activeTab === "active" ? "active" : ""}`}
                        onClick={() => setActiveTab("active")}>
                        <Bike size={20} /> <span>Активне</span>
                        {activeOrder && <div className="sidebar-badge">1</div>}
                    </a>
                    <a href="#" className={`sidebar-item ${activeTab === "history" ? "active" : ""}`}
                        onClick={() => setActiveTab("history")}>
                        <History size={20} /> <span>Історія</span>
                        <div className="sidebar-badge">{history.length}</div>
                    </a>
                </nav>
                <div className="sidebar-bottom">
                    <motion.button
                        whileHover={{ scale: 1.04 }}
                        whileTap={{ scale: 0.98 }}
                        className={`online-toggle ${isOnline ? "online" : "offline"}`}
                        onClick={() => setIsOnline(!isOnline)}
                    >
                        <Power size={18} />
                        {isOnline ? "Онлайн" : "Оффлайн"}
                        <div className="status-dot" />
                    </motion.button>
                    <div className="courier-info">
                        <div className="courier-avatar">
                            {userData?.name?.[0] || "К"}
                        </div>
                        <div>
                            <div className="courier-name">{userData?.name || "Кур’єр"}</div>
                            <div className="courier-earnings">
                                Зароблено сьогодні: <strong>{history.reduce((sum, o) => sum + o.earned, 0) + (activeOrder?.earned || 0)} ₴</strong>
                            </div>
                        </div>
                    </div>
                </div>
            </aside>

            {/* КОНТЕНТ */}
            <div className="auth-homepage courier-homepage">
                <div className="particles">
                    {[...Array(6)].map((_, i) => (
                        <motion.div key={i} className="particle"
                            initial={{ y: -100, x: Math.random() * window.innerWidth }}
                            animate={{ y: window.innerHeight + 100 }}
                            transition={{ duration: 15 + Math.random() * 10, repeat: Infinity, ease: "linear", delay: Math.random() * 5 }}
                        />
                    ))}
                </div>

                <AnimatePresence mode="wait">
                    {/* НОВІ ЗАМОВЛЕННЯ */}
                    {activeTab === "new" && (
                        <motion.section key="new" initial={{ opacity: 0, y: 20 }} animate={{ opacity: 1, y: 0 }} exit={{ opacity: 0, y: -20 }} className="courier-section">
                            <h1 className="gradient-title">{isOnline ? "Доступні замовлення" : "Ви оффлайн"}</h1>
                            {isOnline ? (
                                newOrders.length === 0 ? (
                                    <div className="empty-state">
                                        <Package size={64} strokeWidth={1} />
                                        <p>Нових замовлень немає</p>
                                        <small>Оновлюється автоматично</small>
                                    </div>
                                ) : (
                                    <div className="orders-grid">
                                        {newOrders.map(order => (
                                            <motion.div key={order.id} layout whileHover={{ y: -6 }} className="order-card new-order">
                                                <div className="order-header">
                                                    <h3>{order.restaurant}</h3>
                                                    <div className="order-price">+{Math.round(order.price * 0.25)} ₴</div>
                                                </div>
                                                <div className="order-details">
                                                    <div><MapPin size={14} /> {order.restaurantAddress}</div>
                                                    <div><Clock size={14} /> {order.time} • {order.distance}</div>
                                                    <div><Package size={14} /> {order.items} позицій</div>
                                                </div>
                                                <motion.button whileHover={{ scale: 1.05 }} whileTap={{ scale: 0.95 }} className="accept-btn" onClick={() => acceptOrder(order)}>
                                                    Прийняти замовлення
                                                </motion.button>
                                            </motion.div>
                                        ))}
                                    </div>
                                )
                            ) : (
                                <div className="offline-message">
                                    <Power size={48} />
                                    <p>Увімкніть статус «Онлайн», щоб отримувати замовлення</p>
                                </div>
                            )}
                        </motion.section>
                    )}

                    {/* АКТИВНЕ ЗАМОВЛЕННЯ */}
                    {activeTab === "active" && (
                        <motion.section key="active" initial={{ opacity: 0, y: 20 }} animate={{ opacity: 1, y: 0 }} exit={{ opacity: 0, y: -20 }} className="courier-section active-order-section">
                            {activeOrder ? (
                                <div className="active-order-card">
                                    <div className="active-order-header">
                                        <h2>Замовлення #{activeOrder.id}</h2>
                                        <div className="timer"><Clock size={20} /> {activeOrder.timeLeft}</div>
                                    </div>

                                    <div className="route-steps">
                                        <div className="step">
                                            <div className="step-icon restaurant"><Package size={18} /></div>
                                            <div>
                                                <div className="step-title">{activeOrder.restaurant}</div>
                                                <div className="step-address">{activeOrder.restaurantAddress}</div>
                                                <a href={`tel:${activeOrder.restaurantPhone}`} className="phone-link">
                                                    <Phone size={14} /> {activeOrder.restaurantPhone}
                                                </a>
                                            </div>
                                            {activeOrder.status !== "waiting_pickup" && <CheckCircle className="check" size={20} />}
                                        </div>
                                        <div className="step-connector" />
                                        <div className="step">
                                            <div className="step-icon client"><MapPin size={18} /></div>
                                            <div>
                                                <div className="step-title">{activeOrder.clientName}</div>
                                                <div className="step-address">{activeOrder.clientAddress}</div>
                                                <a href={`tel:${activeOrder.clientPhone}`} className="phone-link">
                                                    <Phone size={14} /> {activeOrder.clientPhone}
                                                </a>
                                            </div>
                                        </div>
                                    </div>

                                    <div className="order-summary">
                                        <div><strong>Страви:</strong> {activeOrder.items} шт</div>
                                        <div><strong>Вартість:</strong> {activeOrder.price} ₴</div>
                                        <div className="earnings">
                                            <DollarSign size={18} /> Ваш заробіток: <strong>{activeOrder.earned} ₴</strong>
                                        </div>
                                    </div>

                                    <div className="active-actions">
                                        {activeOrder.status === "waiting_pickup" && (
                                            <motion.button whileHover={{ scale: 1.03 }} whileTap={{ scale: 0.98 }} className="action-btn primary"
                                                onClick={() => setActiveOrder({ ...activeOrder, status: "picked_up" })}>
                                                Забрав замовлення
                                            </motion.button>
                                        )}
                                        {activeOrder.status === "picked_up" && (
                                            <motion.button whileHover={{ scale: 1.03 }} whileTap={{ scale: 0.98 }} className="action-btn success"
                                                onClick={completeDelivery}>
                                                Доставлено клієнту
                                            </motion.button>
                                        )}
                                    </div>
                                </div>
                            ) : (
                                <div className="empty-state">
                                    <Bike size={64} strokeWidth={1} />
                                    <p>Немає активного замовлення</p>
                                </div>
                            )}
                        </motion.section>
                    )}

                    {/* ІСТОРІЯ */}
                    {activeTab === "history" && (
                        <motion.section key="history" initial={{ opacity: 0, y: 20 }} animate={{ opacity: 1, y: 0 }} className="courier-section">
                            <h1 className="gradient-title">Історія доставок</h1>
                            {history.length === 0 ? (
                                <div className="empty-state">
                                    <History size={56} />
                                    <p>Ви ще не завершили жодної доставки</p>
                                </div>
                            ) : (
                                <div className="orders-grid" style={{ gridTemplateColumns: 'repeat(auto-fill, minmax(360px, 1fr))' }}>
                                    {history.map(order => (
                                        <motion.div key={order.id} layout whileHover={{ y: -4 }} className="order-card history-order">
                                            <div className="order-header">
                                                <div>
                                                    <h3>{order.restaurant}</h3>
                                                    <div style={{ fontSize: '13px', color: 'var(--muted-2)', marginTop: '4px' }}>
                                                        <Calendar size={12} /> {order.date}
                                                    </div>
                                                </div>
                                                <div className="order-price">+{order.earned} ₴</div>
                                            </div>
                                            <div className="order-details" style={{ margin: '12px 0' }}>
                                                <div><MapPin size={14} /> {order.clientName}</div>
                                            </div>
                                            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                                                <div style={{ display: 'flex', gap: '4px' }}>
                                                    {[...Array(order.rating)].map((_, i) => (
                                                        <Star key={i} size={16} fill="gold" stroke="gold" />
                                                    ))}
                                                </div>
                                                <small style={{ color: 'var(--muted)' }}>Замовлення #{order.id}</small>
                                            </div>
                                        </motion.div>
                                    ))}
                                </div>
                            )}
                        </motion.section>
                    )}
                </AnimatePresence>
            </div>
        </div>
    );
}