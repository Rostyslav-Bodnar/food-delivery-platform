import { motion } from "framer-motion";
import {
    Bike,
    CheckCircle,
    Clock,
    DollarSign,
    MapPin,
    Package,
    Phone,
    Navigation
} from "lucide-react";

export default function ActiveOrderSection({
                                               activeOrder,
                                               setActiveOrder,
                                               completeDelivery
                                           }) {
    if (!activeOrder) {
        return (
            <div className="empty-state">
                <Bike size={64} strokeWidth={1} />
                <p>Немає активного замовлення</p>
            </div>
        );
    }

    return (
        <motion.section
            key="active"
            initial={{ opacity: 0, y: 20 }}
            animate={{ opacity: 1, y: 0 }}
            exit={{ opacity: 0, y: -20 }}
            className="courier-section active-order-section"
        >
            <div className="active-order-card">
                <div className="active-order-header">
                    <h2>Замовлення #{activeOrder.id}</h2>
                    <div className="timer">
                        <Clock size={20} /> {activeOrder.timeLeft}
                    </div>
                </div>

                <div className="route-steps">
                    {/* РЕСТОРАН */}
                    <div className="step">
                        <div className="step-icon restaurant">
                            <Package size={18} />
                        </div>
                        <div>
                            <div className="step-title">{activeOrder.restaurant}</div>
                            <div className="step-address">
                                {activeOrder.restaurantAddress}
                            </div>
                            <a
                                href={`tel:${activeOrder.restaurantPhone}`}
                                className="phone-link"
                            >
                                <Phone size={14} /> {activeOrder.restaurantPhone}
                            </a>
                            <a
                                href={`https://www.google.com/maps/search/?api=1&query=${encodeURIComponent(activeOrder.restaurantAddress)}`}
                                target="_blank"
                                rel="noopener noreferrer"
                                className="map-link"
                            >
                                <Navigation size={14} /> Подивитись на карті
                            </a>
                        </div>
                        {activeOrder.status !== "waiting_pickup" && (
                            <CheckCircle className="check" size={20} />
                        )}
                    </div>

                    <div className="step-connector" />

                    {/* КЛІЄНТ */}
                    <div className="step">
                        <div className="step-icon client">
                            <MapPin size={18} />
                        </div>
                        <div>
                            <div className="step-title">{activeOrder.clientName}</div>
                            <div className="step-address">
                                {activeOrder.clientAddress}
                            </div>
                            <a
                                href={`tel:${activeOrder.clientPhone}`}
                                className="phone-link"
                            >
                                <Phone size={14} /> {activeOrder.clientPhone}
                            </a>
                            <a
                                href={`https://www.google.com/maps/search/?api=1&query=${encodeURIComponent(activeOrder.clientAddress)}`}
                                target="_blank"
                                rel="noopener noreferrer"
                                className="map-link"
                            >
                                <Navigation size={14} /> Подивитись на карті
                            </a>
                        </div>
                    </div>
                </div>

                <div className="order-summary">
                    <div>
                        <strong>Страви:</strong> {activeOrder.items} шт
                    </div>
                    <div>
                        <strong>Вартість:</strong> {activeOrder.price} ₴
                    </div>
                    <div>
                        <strong>Відстань:</strong> {activeOrder.distance}
                    </div>
                    <div>
                        <strong>Час створення:</strong> {activeOrder.createdAt}
                    </div>
                    <div className="earnings">
                        <DollarSign size={18} />
                        Ваш заробіток: <strong>{activeOrder.earned} ₴</strong>
                    </div>
                </div>

                <div className="active-actions">
                    {activeOrder.status === "waiting_pickup" && (
                        <motion.button
                            whileHover={{ scale: 1.03 }}
                            whileTap={{ scale: 0.98 }}
                            className="action-btn primary"
                            onClick={() =>
                                setActiveOrder({
                                    ...activeOrder,
                                    status: "picked_up"
                                })
                            }
                        >
                            Забрав замовлення
                        </motion.button>
                    )}

                    {activeOrder.status === "picked_up" && (
                        <motion.button
                            whileHover={{ scale: 1.03 }}
                            whileTap={{ scale: 0.98 }}
                            className="action-btn success"
                            onClick={completeDelivery}
                        >
                            Доставлено клієнту
                        </motion.button>
                    )}
                </div>
            </div>
        </motion.section>
    );
}
