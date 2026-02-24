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
                <Bike size={80} strokeWidth={1} />
                <p>No active order</p>
            </div>
        );
    }

    return (
        <motion.section
            key="active"
            initial={{ opacity: 0, y: 30 }}
            animate={{ opacity: 1, y: 0 }}
            exit={{ opacity: 0, y: -30 }}
            transition={{ duration: 0.5 }}
            className="px-4 pt-8 pb-12"
        >
            <div className="active-order-card">
                {/* Header with order number and timer */}
                <div className="active-order-header">
                    <h2>Order #{activeOrder.id}</h2>
                    <div className="timer">
                        <Clock size={32} />
                        {activeOrder.timeLeft}
                    </div>
                </div>

                {/* Route: Restaurant → Client */}
                <div className="route-steps">
                    {/* Step 1: Restaurant */}
                    <div className="step">
                        <div className="step-icon restaurant">
                            <Package size={24} />
                        </div>
                        <div className="flex-1">
                            <div className="step-title">{activeOrder.restaurant}</div>
                            <div className="step-address">{activeOrder.restaurantAddress}</div>
                            <a href={`tel:${activeOrder.restaurantPhone}`} className="phone-link">
                                <Phone size={16} /> {activeOrder.restaurantPhone}
                            </a>
                            <a
                                href={`https://www.google.com/maps/search/?api=1&query=${encodeURIComponent(activeOrder.restaurantAddress)}`}
                                target="_blank"
                                rel="noopener noreferrer"
                                className="map-link"
                            >
                                <Navigation size={16} /> View on map
                            </a>
                        </div>
                        {activeOrder.status !== "waiting_pickup" && (
                            <CheckCircle className="check" size={32} />
                        )}
                    </div>

                    <div className="step-connector" />

                    {/* Step 2: Client */}
                    <div className="step">
                        <div className="step-icon client">
                            <MapPin size={24} />
                        </div>
                        <div className="flex-1">
                            <div className="step-title">{activeOrder.clientName}</div>
                            <div className="step-address">{activeOrder.clientAddress}</div>
                            <a href={`tel:${activeOrder.clientPhone}`} className="phone-link">
                                <Phone size={16} /> {activeOrder.clientPhone}
                            </a>
                            <a
                                href={`https://www.google.com/maps/search/?api=1&query=${encodeURIComponent(activeOrder.clientAddress)}`}
                                target="_blank"
                                rel="noopener noreferrer"
                                className="map-link"
                            >
                                <Navigation size={16} /> View on map
                            </a>
                        </div>
                    </div>
                </div>

                {/* Order summary */}
                <div className="order-summary">
                    <div>
                        <strong>Items:</strong>
                        <span>{activeOrder.items} pcs</span>
                    </div>
                    <div>
                        <strong>Price:</strong>
                        <span>{activeOrder.price} ₴</span>
                    </div>
                    <div>
                        <strong>Distance:</strong>
                        <span>{activeOrder.distance}</span>
                    </div>
                    <div>
                        <strong>Created at:</strong>
                        <span>{activeOrder.createdAt}</span>
                    </div>

                    <div className="earnings">
                        <DollarSign size={26} />
                        Your earnings: <strong>{activeOrder.earned} ₴</strong>
                    </div>
                </div>

                {/* Action buttons */}
                <div className="active-actions">
                    {activeOrder.status === "waiting_pickup" && (
                        <motion.button
                            whileHover={{ scale: 1.06 }}
                            whileTap={{ scale: 0.96 }}
                            className="action-btn primary"
                            onClick={() =>
                                setActiveOrder({
                                    ...activeOrder,
                                    status: "picked_up"
                                })
                            }
                        >
                            Picked up order
                        </motion.button>
                    )}

                    {activeOrder.status === "picked_up" && (
                        <motion.button
                            whileHover={{ scale: 1.06 }}
                            whileTap={{ scale: 0.96 }}
                            className="action-btn success"
                            onClick={completeDelivery}
                        >
                            Delivered to client
                        </motion.button>
                    )}
                </div>
            </div>
        </motion.section>
    );
}