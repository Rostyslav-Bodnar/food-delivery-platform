import React from "react";
import { motion } from "framer-motion";
import { Clock, MapPin, Package, Phone, DollarSign, Power } from "lucide-react";

export default function NewOrderSection({ isOnline, newOrders, acceptOrder }) {
    return (
        <motion.section
            key="new"
            initial={{ opacity: 0, y: 20 }}
            animate={{ opacity: 1, y: 0 }}
            exit={{ opacity: 0, y: -20 }}
            className="courier-section"
        >
            <h1 className="gradient-title">
                {isOnline ? "Доступні замовлення" : "Ви оффлайн"}
            </h1>

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
                            <motion.div
                                key={order.id}
                                layout
                                whileHover={{ y: -6, boxShadow: "0 12px 32px rgba(0,0,0,0.35)" }}
                                className="order-card new-order"
                            >
                                <div className="order-header">
                                    <h3>{order.restaurant}</h3>
                                    <div className="order-price">
                                        +{Math.round(order.price * 0.25)} ₴
                                    </div>
                                </div>

                                <div className="order-details">
                                    <div><MapPin size={14} /> {order.restaurantAddress}</div>
                                    <div><Phone size={14} /> {order.restaurantPhone}</div>
                                    <div><Clock size={14} /> {order.time} • {order.distance}</div>
                                    <div><Package size={14} /> {order.items} позицій</div>
                                    <div><DollarSign size={14} /> Ваш заробіток: {Math.round(order.price * 0.25)} ₴</div>
                                </div>

                                <div className="order-actions">
                                    <motion.button
                                        whileHover={{ scale: 1.05 }}
                                        whileTap={{ scale: 0.95 }}
                                        className="accept-btn"
                                        onClick={() => acceptOrder(order)}
                                    >
                                        Прийняти замовлення
                                    </motion.button>

                                    <motion.button
                                        whileHover={{ scale: 1.05 }}
                                        whileTap={{ scale: 0.95 }}
                                        href={`https://www.google.com/maps/search/?api=1&query=${encodeURIComponent(order.restaurantAddress)}`}
                                        target="_blank"
                                        rel="noopener noreferrer"
                                        className="map-btn"
                                    >
                                        Подивитись на карті
                                    </motion.button>
                                </div>
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
    );
}
