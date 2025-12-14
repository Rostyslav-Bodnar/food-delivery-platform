// src/pages/courier/CourierHomePage.jsx
import React, { useState, useEffect } from 'react';
import { motion, AnimatePresence } from 'framer-motion';
import {
    Package, MapPin, Phone, Clock, CheckCircle, Bike,
    DollarSign, History, Power, Calendar, Star
} from 'lucide-react';
import CourierSidebar from './components/CourierSidebar.jsx';
import NewOrderSection from './components/NewOrdersSection.jsx';
import ActiveOrderSection from './components/ActiveOrderSection.jsx';
import HistorySection from './components/HistorySection.jsx';

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
            <CourierSidebar
                activeTab={activeTab}
                setActiveTab={setActiveTab}
                activeOrder={activeOrder}
                history={history}
                isOnline={isOnline}
                setIsOnline={setIsOnline}
                userData={userData}
            />


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
                        <NewOrderSection
                            isOnline={isOnline}
                            newOrders={newOrders}
                            acceptOrder={acceptOrder}
                        />
                    )}

                    {/* АКТИВНЕ ЗАМОВЛЕННЯ */}
                    {activeTab === "active" && (
                        <ActiveOrderSection
                            activeOrder={activeOrder}
                            setActiveOrder={setActiveOrder}
                            completeDelivery={completeDelivery}
                        />
                    )}

                    {/* ІСТОРІЯ */}
                    {activeTab === "history" && (
                        <HistorySection history={history} />
                    )}

                </AnimatePresence>
            </div>
        </div>
    );
}