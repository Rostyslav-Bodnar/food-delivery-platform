import React from "react";
import { motion } from "framer-motion";
import { Clock, MapPin, Package, Phone, Power } from "lucide-react";

export default function NewOrderSection({ isOnline, newOrders, acceptOrder }) {

    if (!isOnline) {
        return (
            <motion.section
                key="offline"
                initial={{ opacity: 0, y: 20 }}
                animate={{ opacity: 1, y: 0 }}
                exit={{ opacity: 0, y: -20 }}
                className="courier-section"
            >
                <h1 className="gradient-title">Ви офлайн</h1>

                <div className="offline-message">
                    <Power size={48} />
                    <p>Увімкніть статус «Онлайн», щоб отримувати замовлення</p>
                </div>
            </motion.section>
        );
    }

    return (
        <motion.section
            key="new"
            initial={{ opacity: 0, y: 20 }}
            animate={{ opacity: 1, y: 0 }}
            exit={{ opacity: 0, y: -20 }}
            className="courier-section"
        >
            <h1 className="gradient-title">Доступні замовлення</h1>

            {newOrders.length === 0 ? (
                <div className="empty-state">
                    <Package size={64} strokeWidth={1} />
                    <p>Нових замовлень немає</p>
                    <small>Оновлюється автоматично</small>
                </div>
            ) : (
                <div className="orders-grid">
                    {newOrders.map(order => {
                        const earned = Math.round(order.totalPrice * 0.25);

                        return (
                            <motion.div
                                key={order.id}
                                layout
                                whileHover={{
                                    y: -6,
                                    boxShadow: "0 12px 32px rgba(0,0,0,0.35)"
                                }}
                                className="order-card new-order"
                            >
                                {/* HEADER */}
                                <div className="order-header">
                                    <h3>{order.businessName}</h3>
                                    <div className="order-price">
                                        +{earned} ₴
                                    </div>
                                </div>

                                {/* DETAILS */}
                                <div className="order-details">
                                    <div>
                                        <MapPin size={14} />
                                        {order.customerAddress || "Адреса не вказана"}
                                    </div>

                                    <div>
                                        <Phone size={14} />
                                        {order.customerPhoneNumber || "Телефон не вказаний"}
                                    </div>

                                    <div>
                                        <Clock size={14} />
                                        {new Date(order.orderDate).toLocaleTimeString("uk-UA", {
                                            hour: "2-digit",
                                            minute: "2-digit"
                                        })}
                                    </div>
                                </div>

                                {/* ACTIONS */}
                                <div className="order-actions">
                                    <motion.button
                                        whileHover={{ scale: 1.05 }}
                                        whileTap={{ scale: 0.95 }}
                                        className="accept-btn"
                                        onClick={() => acceptOrder(order)}
                                    >
                                        Прийняти замовлення
                                    </motion.button>

                                    {order.customerAddress && (
                                        <motion.a
                                            whileHover={{ scale: 1.05 }}
                                            whileTap={{ scale: 0.95 }}
                                            href={`https://www.google.com/maps/search/?api=1&query=${encodeURIComponent(order.customerAddress)}`}
                                            target="_blank"
                                            rel="noopener noreferrer"
                                            className="map-btn"
                                        >
                                            Подивитись на карті
                                        </motion.a>
                                    )}
                                </div>
                            </motion.div>
                        );
                    })}
                </div>
            )}
        </motion.section>
    );
}
