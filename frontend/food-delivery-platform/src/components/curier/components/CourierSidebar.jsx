import { motion } from 'framer-motion';
import { Package, Bike, History, Power } from 'lucide-react';

export default function CourierSidebar({
                                           activeTab,
                                           setActiveTab,
                                           activeOrder,
                                           history,
                                           isOnline,
                                           setIsOnline,
                                           userData
                                       }) {
    return (
        <aside className="courier-sidebar">
            <div className="sidebar-logo">ШвидкоДоставка</div>

            <nav className="sidebar-nav">
                <a
                    href="#"
                    className={`sidebar-item ${activeTab === "new" ? "active" : ""}`}
                    onClick={() => setActiveTab("new")}
                >
                    <Package size={20} />
                    <span>Нові замовлення</span>
                </a>

                <a
                    href="#"
                    className={`sidebar-item ${activeTab === "active" ? "active" : ""}`}
                    onClick={() => setActiveTab("active")}
                >
                    <Bike size={20} />
                    <span>Активне</span>
                    {activeOrder && <div className="sidebar-badge">1</div>}
                </a>

                <a
                    href="#"
                    className={`sidebar-item ${activeTab === "history" ? "active" : ""}`}
                    onClick={() => setActiveTab("history")}
                >
                    <History size={20} />
                    <span>Історія</span>
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
                        <div className="courier-name">
                            {userData?.name || "Кур’єр"}
                        </div>
                        <div className="courier-earnings">
                            Зароблено сьогодні:{" "}
                            <strong>
                                {history.reduce((sum, o) => sum + o.earned, 0)
                                    + (activeOrder?.earned || 0)} ₴
                            </strong>
                        </div>
                    </div>
                </div>
            </div>
        </aside>
    );
}
