// src/hooks/useOrderDelivery.js
import { useState, useEffect } from "react";
import { getOrdersByCourier } from "../../../../api/Order.jsx";

const useOrderDelivery = (userData) => {
    const [activeTab, setActiveTab] = useState("new");
    const [activeOrder, setActiveOrder] = useState(null);
    const [newOrders, setNewOrders] = useState([]);
    const [history, setHistory] = useState([]);

    const fetchNewOrders = async () => {
        if (!userData?.currentAccount?.id) return;

        try {
            const orders = await getOrdersByCourier(userData.currentAccount.id);
            setNewOrders(orders);
        } catch (err) {
            console.error("Error while loading orders:", err);
        }
    };

    useEffect(() => {
        fetchNewOrders();

        const interval = setInterval(fetchNewOrders, 30000);
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
            date:
                "Today, " +
                new Date().toLocaleTimeString("uk-UA", {
                    hour: "2-digit",
                    minute: "2-digit"
                }),
            rating: Math.floor(Math.random() * 2) + 4
        };

        setHistory(prev => [completedOrder, ...prev]);
        alert(`Delivery completed! +${activeOrder.earned} ₴ added to balance`);

        setActiveOrder(null);
        setActiveTab("new");
    };

    return {
        activeTab,
        setActiveTab,
        activeOrder,
        setActiveOrder,
        newOrders,
        history,
        acceptOrder,
        completeDelivery
    };
};

export default useOrderDelivery;