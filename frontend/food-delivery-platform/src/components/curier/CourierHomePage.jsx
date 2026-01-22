// src/pages/courier/CourierHomePage.jsx
import React, { useState, useEffect } from 'react';
import { motion, AnimatePresence } from 'framer-motion';
import {
    Package, MapPin, Phone, Clock, DollarSign, Power
} from 'lucide-react';
import CourierSidebar from '../sidebars/CourierSidebar.jsx';
import NewOrderSection from './components/NewOrdersSection.jsx';
import ActiveOrderSection from './components/ActiveOrderSection.jsx';
import HistorySection from './components/HistorySection.jsx';

import { getOrdersByCourier } from '../../api/Order.jsx';

import './styles/CourierHomePage.css';

export default function CourierHomePage({ userData }) {
    const [isOnline, setIsOnline] = useState(true);
    const [activeTab, setActiveTab] = useState("new");
    const [activeOrder, setActiveOrder] = useState(null);
    const [newOrders, setNewOrders] = useState([]);
    const [history, setHistory] = useState([]);

    // Завантажуємо нові замовлення з бекенду
    useEffect(() => {
        console.log(userData);
        if (!userData || !userData.id) return;

        const fetchNewOrders = async () => {
            try {
                const orders = await getOrdersByCourier(userData.currentAccount?.id);
                // Фільтруємо тільки замовлення, які ще не прийняті
                setNewOrders(orders);
            } catch (err) {
                console.error("Помилка при завантаженні замовлень:", err);
            }
        };

        fetchNewOrders();

        // Можна додати інтервал для автоматичного оновлення
        const interval = setInterval(fetchNewOrders, 30000); // оновлюємо кожні 30 сек
        return () => clearInterval(interval);

    }, [userData]);

    const acceptOrder = (order) => {
        setActiveOrder({
            ...order,
            clientAddress: order.customerAddress,
            clientName: order.customerFullName,
            clientPhone: order.customerPhoneNumber,
            earned: Math.round(order.totalPrice * 0.25),
            timeLeft: "20:00",
            status: "waiting_pickup"
        });
        setNewOrders(prev => prev.filter(o => o.id !== order.id));
        setActiveTab("active");
    };

    const completeDelivery = () => {
        if (!activeOrder) return;

        const completedOrder = {
            id: activeOrder.id,
            restaurant: activeOrder.businessName,
            clientName: activeOrder.clientName,
            earned: activeOrder.earned,
            price: activeOrder.totalPrice,
            date: "Сьогодні, " + new Date().toLocaleTimeString('uk-UA', { hour: '2-digit', minute: '2-digit' }),
            rating: Math.floor(Math.random() * 2) + 4
        };

        setHistory(prev => [completedOrder, ...prev]);
        alert(`Доставку завершено! +${activeOrder.earned} ₴ на баланс`);
        setActiveOrder(null);
        setActiveTab("new");
    };

    return (
        <div className="app-wrapper">
            <CourierSidebar
                activeTab={activeTab}
                setActiveTab={setActiveTab}
                activeOrder={activeOrder}
                history={history}
                isOnline={isOnline}
                setIsOnline={setIsOnline}
                userData={userData}
            />

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
                    {activeTab === "new" && (
                        <NewOrderSection
                            isOnline={isOnline}
                            newOrders={newOrders}
                            acceptOrder={acceptOrder}
                        />
                    )}

                    {activeTab === "active" && (
                        <ActiveOrderSection
                            activeOrder={activeOrder}
                            setActiveOrder={setActiveOrder}
                            completeDelivery={completeDelivery}
                        />
                    )}

                    {activeTab === "history" && (
                        <HistorySection history={history} />
                    )}
                </AnimatePresence>
            </div>
        </div>
    );
}
