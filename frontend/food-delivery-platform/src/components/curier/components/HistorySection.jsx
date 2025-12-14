import { motion } from "framer-motion";
import { Calendar, History, MapPin, Star } from "lucide-react";

export default function HistorySection({ history }) {
    if (!history || history.length === 0) {
        return (
            <div className="empty-state">
                <History size={56} />
                <p>Ви ще не завершили жодної доставки</p>
            </div>
        );
    }

    return (
        <motion.section
            key="history"
            initial={{ opacity: 0, y: 20 }}
            animate={{ opacity: 1, y: 0 }}
            exit={{ opacity: 0, y: -20 }}
            className="courier-section"
        >
            <h1 className="gradient-title">Історія доставок</h1>
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
        </motion.section>
    );
}
