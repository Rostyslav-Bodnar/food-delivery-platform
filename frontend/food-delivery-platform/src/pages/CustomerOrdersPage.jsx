import React, { useState, useEffect } from "react";
import { Link } from "react-router-dom";
import "./styles/CustomerOrdersPage.css";
import CustomerSidebar from "../components/sidebars/CustomerSidebar";
import OrderDetailsComponent from "../components/OrderDetailsComponent.jsx";
import { getCustomerOrders } from "../api/Order.jsx";

const CustomerOrdersPage = () => {
    const [orders, setOrders] = useState([]);
    const [loading, setLoading] = useState(true);
    const [selectedOrder, setSelectedOrder] = useState(null);

    const customerId = localStorage.getItem("currentAccountId"); // або з AuthContext

    useEffect(() => {
        const loadOrders = async () => {
            try {
                const data = await getCustomerOrders(customerId);

                const mappedOrders = data.map(o => ({
                    id: o.id,
                    restaurant: o.businessName,
                    address: o.businessAddress,
                    status: mapStatus(o.orderStatus),
                    total: o.totalPrice,
                    createdAt: new Date(o.orderDate).toLocaleString(),
                    courier: o.courierName
                        ? { name: o.courierName }
                        : null,
                    items: o.dishes.map(d => ({
                        name: d.dishName,
                        quantity: d.quantity,
                        price: d.price
                    }))
                }));

                setOrders(mappedOrders);
            } catch (e) {
                console.error("Failed to load orders", e);
            } finally {
                setLoading(false);
            }
        };

        loadOrders();
    }, [customerId]);

    const getStatus = (status) => {
        if (status === "preparing") return { text: "Готується", color: "#ffb86b", icon: "🍳" };
        if (status === "on-the-way") return { text: "В дорозі", color: "#00d4ff", icon: "🏍️" };
        return { text: "Нове", color: "#7c5cff", icon: "📦" };
    };

    return (
        <div className="app-wrapper">
            <CustomerSidebar />

            <main className="auth-homepage customer-orders-page">
                <div className="page-header">
                    <h1 className="gradient-title">Активні замовлення</h1>
                </div>

                {loading ? (
                    <div className="active-orders-list">
                        <div className="active-order-card skeleton"></div>
                        <div className="active-order-card skeleton"></div>
                    </div>
                ) : orders.length === 0 ? (
                    <div className="no-active-orders">
                        <div className="big-icon">🍕</div>
                        <h3>Немає активних замовлень</h3>
                        <p>Коли ви замовите їжу — статус з’явиться тут</p>
                        <Link to="/" className="big-cta-btn">Замовити зараз</Link>
                    </div>
                ) : (
                    <div className="active-orders-list">
                        {orders.map(order => {
                            const s = getStatus(order.status);

                            return (
                                <div key={order.id} className="active-order-card">
                                    <div className="active-order-header">
                                        <div className="order-id">#{order.id.slice(0, 8)}</div>
                                        <div className="live-status" style={{ color: s.color }}>
                                            {s.icon} {s.text}
                                        </div>
                                    </div>

                                    <div className="active-order-body">
                                        <h3>{order.restaurant}</h3>

                                        <div className="order-items-list">
                                            {order.items.map((item, idx) => (
                                                <div key={idx} className="order-item">
                                                    <span className="item-name">{item.name}</span>
                                                    <span className="item-details">
                                                        {item.quantity} × {item.price} ₴
                                                    </span>
                                                </div>
                                            ))}
                                        </div>

                                        {order.status === "on-the-way" && order.courier && (
                                            <div className="courier-info">
                                                <div className="courier-avatar">👤</div>
                                                <div className="courier-name">{order.courier.name}</div>
                                            </div>
                                        )}

                                        <div style={{
                                            padding: "20px 0 0",
                                            borderTop: "1px solid var(--glass-border)",
                                            marginTop: "20px"
                                        }}>
                                            <div style={{
                                                display: "flex",
                                                justifyContent: "flex-end",
                                                fontSize: "20px",
                                                fontWeight: "700"
                                            }}>
                                                До сплати: {order.total} ₴
                                            </div>
                                        </div>
                                    </div>

                                    <div className="active-order-footer">
                                        <button className="track-btn">Відстежити на карті</button>
                                        <button
                                            className="details-btn"
                                            onClick={() => setSelectedOrder(order)}
                                        >
                                            Деталі замовлення
                                        </button>
                                    </div>
                                </div>
                            );
                        })}
                    </div>
                )}
            </main>

            {selectedOrder && (
                <OrderDetailsComponent
                    order={selectedOrder}
                    statusMap={{
                        preparing: { label: "Готується", icon: () => <span>🍳</span> },
                        "on-the-way": { label: "В дорозі", icon: () => <span>🏍️</span> },
                        new: { label: "Нове", icon: () => <span>📦</span> }
                    }}
                    onClose={() => setSelectedOrder(null)}
                />
            )}
        </div>
    );
};

const mapStatus = (status) => {
    switch (status) {
        case "Preparing": return "preparing";
        case "OnTheWay": return "on-the-way";
        default: return "new";
    }
};

export default CustomerOrdersPage;
