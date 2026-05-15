import React, { useState } from "react";
import { Link } from "react-router-dom";
import { ArrowRight, Bike, Clock3, Package, Wallet } from "lucide-react";

import CourierSidebar from "../../sidebars/CourierSidebar.jsx";
import useOrderDelivery from "./hooks/useOrderDelivery.jsx";
import { formatMoney } from "../../courier-orders/courierOrderUtils.js";

import "../styles/CourierHomePage.css";

const formatOrderDate = (value) =>
    new Date(value).toLocaleString("uk-UA", {
        day: "2-digit",
        month: "2-digit",
        hour: "2-digit",
        minute: "2-digit"
    });

export default function CourierHomePage({ userData }) {
    const [isOnline, setIsOnline] = useState(true);

    const {
        loading,
        availableOrders,
        activeOrder,
        history,
        stats,
        activeStage
    } = useOrderDelivery(userData);

    return (
        <div className="courier-dashboard-shell">
            <CourierSidebar
                isOnline={isOnline}
                setIsOnline={setIsOnline}
                userData={userData}
                availableCount={stats.availableCount}
                activeCount={stats.activeCount}
            />

            <main className="courier-dashboard-page">
                <section className="courier-dashboard-hero">
                    <div className="courier-dashboard-hero__copy">
                        <span className="courier-dashboard-eyebrow">Courier workspace</span>
                        <h1>Fast dispatch, clear priorities, and route-first delivery flow.</h1>
                        <p>
                            The dashboard highlights what matters now: ready orders nearby,
                            your active delivery leg, and completed jobs.
                        </p>

                        <div className="courier-dashboard-hero__actions">
                            <Link to="/courier/orders" className="courier-dashboard-cta">
                                Open orders board
                                <ArrowRight size={16} />
                            </Link>
                            <div className={`courier-dashboard-status ${isOnline ? "is-online" : "is-offline"}`}>
                                {isOnline ? "Online for dispatch" : "Offline"}
                            </div>
                        </div>
                    </div>

                    <div className="courier-dashboard-highlight">
                        <span>Active leg</span>
                        <strong>
                            {activeOrder
                                ? activeStage === "pickup"
                                    ? "Heading to restaurant"
                                    : "Heading to customer"
                                : "Waiting for assignment"}
                        </strong>
                        <p>
                            {activeOrder
                                ? `${activeOrder.businessName} -> ${activeOrder.customerAddress || "Customer"}`
                                : "Pick the next order from the Orders page."}
                        </p>
                    </div>
                </section>

                <section className="courier-dashboard-stats">
                    <article className="courier-stat-card">
                        <Package size={18} />
                        <span>Ready orders</span>
                        <strong>{loading ? "..." : stats.availableCount}</strong>
                    </article>
                    <article className="courier-stat-card">
                        <Bike size={18} />
                        <span>Active deliveries</span>
                        <strong>{loading ? "..." : stats.activeCount}</strong>
                    </article>
                    <article className="courier-stat-card">
                        <Clock3 size={18} />
                        <span>Completed</span>
                        <strong>{loading ? "..." : stats.deliveredCount}</strong>
                    </article>
                    <article className="courier-stat-card">
                        <Wallet size={18} />
                        <span>Total courier fee</span>
                        <strong>{loading ? "..." : formatMoney(stats.totalRevenue)}</strong>
                    </article>
                </section>

                <section className="courier-dashboard-grid">
                    <article className="courier-dashboard-card courier-dashboard-card--accent">
                        <div className="courier-dashboard-card__header">
                            <div>
                                <span>Dispatch now</span>
                                <h2>Nearest ready orders</h2>
                            </div>
                            <Link to="/courier/orders">See all</Link>
                        </div>

                        {availableOrders.length === 0 ? (
                            <div className="courier-dashboard-empty">No ready orders right now.</div>
                        ) : (
                            <div className="courier-dashboard-list">
                                {availableOrders.slice(0, 3).map((order) => (
                                    <div key={order.id} className="courier-dashboard-list__item">
                                        <div>
                                            <strong>{order.businessName}</strong>
                                            <p>{order.customerAddress || "Customer address unavailable"}</p>
                                        </div>
                                        <span>{formatMoney(order.totalPrice)}</span>
                                    </div>
                                ))}
                            </div>
                        )}
                    </article>

                    <article className="courier-dashboard-card">
                        <div className="courier-dashboard-card__header">
                            <div>
                                <span>In progress</span>
                                <h2>Current delivery</h2>
                            </div>
                        </div>

                        {!activeOrder ? (
                            <div className="courier-dashboard-empty">No active order assigned.</div>
                        ) : (
                            <div className="courier-dashboard-route">
                                <div className={`courier-dashboard-route__step ${activeStage === "pickup" ? "is-current" : "is-done"}`}>
                                    <strong>{activeOrder.businessName}</strong>
                                    <p>{activeOrder.businessAddress || "Restaurant address unavailable"}</p>
                                </div>
                                <div className={`courier-dashboard-route__step ${activeStage === "dropoff" ? "is-current" : ""}`}>
                                    <strong>Customer</strong>
                                    <p>{activeOrder.customerAddress || "Customer address unavailable"}</p>
                                </div>
                                <Link to="/courier/orders" className="courier-dashboard-inline-link">
                                    Open live map
                                </Link>
                            </div>
                        )}
                    </article>

                    <article className="courier-dashboard-card">
                        <div className="courier-dashboard-card__header">
                            <div>
                                <span>Recent jobs</span>
                                <h2>Delivery history</h2>
                            </div>
                        </div>

                        {history.length === 0 ? (
                            <div className="courier-dashboard-empty">Your completed deliveries will show up here.</div>
                        ) : (
                            <div className="courier-dashboard-list">
                                {history.slice(0, 4).map((order) => (
                                    <div key={order.id} className="courier-dashboard-list__item">
                                        <div>
                                            <strong>{order.businessName}</strong>
                                            <p>{formatOrderDate(order.orderDate)}</p>
                                        </div>
                                        <span>{formatMoney(order.profit)}</span>
                                    </div>
                                ))}
                            </div>
                        )}
                    </article>
                </section>
            </main>
        </div>
    );
}
