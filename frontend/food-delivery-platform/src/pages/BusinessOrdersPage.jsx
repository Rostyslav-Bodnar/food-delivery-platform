import React, { useEffect, useState } from "react";
import {
    Package,
    Clock,
    CheckCircle,
    XCircle,
    ChevronRight
} from "lucide-react";
import "./styles/BusinessOrdersPage.css";
import BusinessSidebar from "../components/business/BusinessSidebar.jsx";
import OrderDetailsComponent from "../components/OrderDetailsComponent.jsx";
import {
    getOrdersByBusiness,
    changeOrderStatus
} from "../api/Order.jsx";

const STATUS_MAP = {
    pending: { label: "Нове", color: "#7c5cff", icon: Package },
    preparing: { label: "Готується", color: "#ffb86b", icon: Clock },
    ready: { label: "Готове", color: "#00d4ff", icon: CheckCircle },
    delivered: { label: "Доставлено", color: "#50fa7b", icon: CheckCircle },
    cancelled: { label: "Скасовано", color: "#ff6b6b", icon: XCircle },
};

const BACKEND_STATUS_MAP = {
    Preparing: "preparing",
    Ready: "ready",
    OutForDelivery: "ready",
    Delivered: "delivered",
    Canceled: "cancelled"
};

const FRONT_TO_BACK_STATUS = {
    preparing: "Preparing",
    ready: "Ready",
    delivered: "Delivered",
    cancelled: "Canceled"
};

export default function BusinessOrdersPage({ userData }) {
    const [orders, setOrders] = useState([]);
    const [filter, setFilter] = useState("all");
    const [selectedOrder, setSelectedOrder] = useState(null);
    const [loading, setLoading] = useState(true);

    const businessId = localStorage.getItem("currentAccountId");

    useEffect(() => {
        if (!businessId) return;

        const loadOrders = async () => {
            try {
                const data = await getOrdersByBusiness(businessId);

                const mapped = data.map(o => ({
                    id: o.id,
                    createdAt: new Date(o.orderDate).toLocaleTimeString([], {
                        hour: "2-digit",
                        minute: "2-digit"
                    }),
                    customerName: o.customerFullName,
                    address: o.customerAddress,
                    total: o.totalPrice,
                    status: BACKEND_STATUS_MAP[o.orderStatus] ?? "pending",
                    courier: o.courierName
                        ? { name: o.courierName }
                        : null,
                    items: o.dishes.map(d => ({
                        name: d.dishName,
                        quantity: d.quantity,
                        price: d.price
                    }))
                }));

                setOrders(mapped);
            } catch (e) {
                console.error("Failed to load business orders", e);
            } finally {
                setLoading(false);
            }
        };

        loadOrders();
    }, [businessId]);

    const filteredOrders =
        filter === "all"
            ? orders
            : orders.filter(o => o.status === filter);

    const handleStatusChange = async (orderId, newStatus) => {
        try {
            await changeOrderStatus(orderId, FRONT_TO_BACK_STATUS[newStatus]);

            setOrders(prev =>
                prev.map(o =>
                    o.id === orderId
                        ? { ...o, status: newStatus }
                        : o
                )
            );
        } catch (e) {
            console.error("Failed to change order status", e);
        }
    };

    return (
        <div className="bh-page">
            <BusinessSidebar userData={userData} />

            <main className="bh-main">
                <header className="bh-top">
                    <h1 className="bh-heading">Замовлення</h1>

                    <div className="filters">
                        <select value={filter} onChange={e => setFilter(e.target.value)}>
                            <option value="all">Усі</option>
                            <option value="pending">Нові</option>
                            <option value="preparing">Готується</option>
                            <option value="ready">Готові</option>
                            <option value="delivered">Доставлені</option>
                            <option value="cancelled">Скасовані</option>
                        </select>
                    </div>
                </header>

                <section className="bh-content">
                    {loading ? (
                        <div className="bh-empty">Завантаження...</div>
                    ) : filteredOrders.length === 0 ? (
                        <div className="bh-empty">
                            <Package size={64} />
                            <p>Немає замовлень</p>
                        </div>
                    ) : (
                        <div className="orders-grid">
                            {filteredOrders.map(order => {
                                const s = STATUS_MAP[order.status];
                                const StatusIcon = s.icon;

                                return (
                                    <div key={order.id} className="order-card">
                                        <div className="order-header">
                                            <div className="order-id">#{order.id.slice(0, 8)}</div>
                                            <div className="order-time">{order.createdAt}</div>
                                        </div>

                                        <div className="order-body">
                                            <strong>{order.customerName}</strong>
                                            <div className="address">{order.address}</div>

                                            <div className="order-items">
                                                {order.items.map((item, i) => (
                                                    <div key={i} className="item-row">
                                                        <span>{item.quantity}× {item.name}</span>
                                                        <span>{item.price * item.quantity} ₴</span>
                                                    </div>
                                                ))}
                                            </div>

                                            <div className="order-total">
                                                Разом: {order.total} ₴
                                            </div>
                                        </div>

                                        <div className="order-footer">
                                            <div
                                                className="status-badge"
                                                style={{ background: s.color + "22", color: s.color }}
                                            >
                                                <StatusIcon size={16} />
                                                {s.label}
                                            </div>

                                            <div className="status-actions">
                                                {order.status === "pending" && (
                                                    <>
                                                        <button onClick={() => handleStatusChange(order.id, "preparing")}>
                                                            Прийняти
                                                        </button>
                                                        <button
                                                            className="danger"
                                                            onClick={() => handleStatusChange(order.id, "cancelled")}
                                                        >
                                                            Скасувати
                                                        </button>
                                                    </>
                                                )}

                                                {order.status === "preparing" && (
                                                    <button onClick={() => handleStatusChange(order.id, "ready")}>
                                                        Готово
                                                    </button>
                                                )}

                                                {order.status === "ready" && (
                                                    <button onClick={() => handleStatusChange(order.id, "delivered")}>
                                                        Видано
                                                    </button>
                                                )}
                                            </div>

                                            <button
                                                className="details-btn"
                                                onClick={() => setSelectedOrder(order)}
                                            >
                                                Детальніше <ChevronRight size={16} />
                                            </button>
                                        </div>
                                    </div>
                                );
                            })}
                        </div>
                    )}
                </section>
            </main>

            {selectedOrder && (
                <OrderDetailsComponent
                    order={selectedOrder}
                    statusMap={STATUS_MAP}
                    onClose={() => setSelectedOrder(null)}
                />
            )}
        </div>
    );
}
