import React, { useState } from "react";
import { motion, AnimatePresence } from "framer-motion";

import CourierSidebar from "../../sidebars/CourierSidebar.jsx";
import NewOrderSection from "./components/NewOrdersSection.jsx";
import ActiveOrderSection from "./components/ActiveOrderSection.jsx";
import HistorySection from "./components/HistorySection.jsx";

import useOrderDelivery from "./hooks/useOrderDelivery";

import "../styles/CourierHomePage.css";

export default function CourierHomePage({ userData }) {
    const [isOnline, setIsOnline] = useState(true);

    const {
        activeTab,
        setActiveTab,
        activeOrder,
        setActiveOrder,
        newOrders,
        history,
        acceptOrder,
        completeDelivery
    } = useOrderDelivery(userData);

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
                        <motion.div
                            key={i}
                            className="particle"
                            initial={{ y: -100, x: Math.random() * window.innerWidth }}
                            animate={{ y: window.innerHeight + 100 }}
                            transition={{
                                duration: 15 + Math.random() * 10,
                                repeat: Infinity,
                                ease: "linear",
                                delay: Math.random() * 5
                            }}
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