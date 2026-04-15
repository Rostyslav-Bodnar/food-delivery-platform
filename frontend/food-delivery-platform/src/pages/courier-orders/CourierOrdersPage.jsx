import React, { useEffect, useMemo, useState } from "react";
import {
    Bike,
    Clock3,
    LocateFixed,
    MapPin,
    Navigation,
    Package,
    Route,
    Store
} from "lucide-react";

import CourierSidebar from "../sidebars/CourierSidebar.jsx";
import {
    changeOrderStatus,
    deliverOrder,
    getActiveCourierOrders,
    getAvailableCourierOrders,
    getCourierOrderHistory
} from "../../api/Order.jsx";
import { hasCoordinates } from "../../utils/orderLocations.js";
import { getRoadRoute } from "../../utils/roadRouting.js";
import CourierRouteMap from "./components/CourierRouteMap.jsx";
import {
    calculateDistanceKm,
    clearCourierDeliveryStage,
    formatMoney,
    formatRouteDistance,
    formatRouteDuration,
    getCourierDeliveryStage,
    mapCourierOrder,
    setCourierDeliveryStage
} from "./courierOrderUtils.js";
import "./styles/CourierOrdersPage.css";

const STAGE_TO_STATUS = {
    pickup: "Ready",
    dropoff: "OutForDelivery"
};

const markerPalette = {
    courier: { fillColor: "#7c5cff", strokeColor: "#d9d2ff" },
    business: { fillColor: "#ff6b6b", strokeColor: "#ffd0d0" },
    customer: { fillColor: "#00d4ff", strokeColor: "#bff6ff" }
};

export default function CourierOrdersPage() {
    const courierId = localStorage.getItem("currentAccountId");
    const courierName = localStorage.getItem("currentAccountName") ?? "Courier";

    const [isOnline, setIsOnline] = useState(true);
    const [availableOrders, setAvailableOrders] = useState([]);
    const [activeOrders, setActiveOrders] = useState([]);
    const [history, setHistory] = useState([]);
    const [courierPosition, setCourierPosition] = useState(null);
    const [locationError, setLocationError] = useState("");
    const [route, setRoute] = useState(null);
    const [actionLoading, setActionLoading] = useState("");
    const [loading, setLoading] = useState(true);

    const activeOrder = activeOrders[0] ?? null;
    const deliveryStage = activeOrder
        ? getCourierDeliveryStage(activeOrder.id, activeOrder.orderStatus)
        : "pickup";

    useEffect(() => {
        if (!navigator.geolocation) {
            setLocationError("Geolocation is unavailable in this browser.");
            return undefined;
        }

        const watchId = navigator.geolocation.watchPosition(
            ({ coords }) => {
                setCourierPosition({
                    latitude: coords.latitude,
                    longitude: coords.longitude
                });
                setLocationError("");
            },
            () => {
                setLocationError("Allow geolocation to sort nearby orders and build the route.");
            },
            {
                enableHighAccuracy: true,
                maximumAge: 15000,
                timeout: 10000
            }
        );

        return () => navigator.geolocation.clearWatch(watchId);
    }, []);

    useEffect(() => {
        if (!courierId) {
            setLoading(false);
            return;
        }

        let isMounted = true;

        const loadOrders = async () => {
            try {
                setLoading(true);
                const [available, active, historyData] = await Promise.all([
                    getAvailableCourierOrders(courierId),
                    getActiveCourierOrders(courierId),
                    getCourierOrderHistory(courierId)
                ]);

                if (!isMounted) {
                    return;
                }

                setAvailableOrders(available.map(mapCourierOrder));
                setActiveOrders(active.map(mapCourierOrder));
                setHistory(historyData.map(mapCourierOrder));
            } catch (error) {
                console.error("Failed to load courier orders", error);
            } finally {
                if (isMounted) {
                    setLoading(false);
                }
            }
        };

        loadOrders();
        const intervalId = window.setInterval(loadOrders, 30000);

        return () => {
            isMounted = false;
            window.clearInterval(intervalId);
        };
    }, [courierId]);

    useEffect(() => {
        let isMounted = true;

        const buildRoute = async () => {
            if (!activeOrder) {
                setRoute(null);
                return;
            }

            const start =
                deliveryStage === "pickup"
                    ? courierPosition
                    : activeOrder.businessLocation;
            const end =
                deliveryStage === "pickup"
                    ? activeOrder.businessLocation
                    : activeOrder.customerLocation;

            if (!hasCoordinates(start) || !hasCoordinates(end)) {
                setRoute(null);
                return;
            }

            try {
                const nextRoute = await getRoadRoute(start, end);
                if (isMounted) {
                    setRoute(nextRoute);
                }
            } catch (error) {
                console.error("Failed to build courier route", error);
                if (isMounted) {
                    setRoute(null);
                }
            }
        };

        buildRoute();

        return () => {
            isMounted = false;
        };
    }, [activeOrder, courierPosition, deliveryStage]);

    const sortedAvailableOrders = useMemo(() => {
        return [...availableOrders]
            .map((order) => ({
                ...order,
                distanceToBusinessKm: calculateDistanceKm(courierPosition, order.businessLocation)
            }))
            .sort((first, second) => {
                if (first.distanceToBusinessKm == null) {
                    return 1;
                }

                if (second.distanceToBusinessKm == null) {
                    return -1;
                }

                return first.distanceToBusinessKm - second.distanceToBusinessKm;
            });
    }, [availableOrders, courierPosition]);

    const historyPreview = useMemo(() => history.slice(0, 4), [history]);

    const routeMarkers = useMemo(() => {
        if (!activeOrder) {
            return [];
        }

        const markers = [];

        if (deliveryStage === "pickup" && hasCoordinates(courierPosition)) {
            markers.push({
                key: "courier",
                label: "Your location",
                position: [courierPosition.latitude, courierPosition.longitude],
                ...markerPalette.courier
            });
        }

        if (hasCoordinates(activeOrder.businessLocation)) {
            markers.push({
                key: "business",
                label: activeOrder.businessAddress,
                position: [activeOrder.businessLocation.latitude, activeOrder.businessLocation.longitude],
                ...markerPalette.business
            });
        }

        if (hasCoordinates(activeOrder.customerLocation)) {
            markers.push({
                key: "customer",
                label: activeOrder.customerAddress,
                position: [activeOrder.customerLocation.latitude, activeOrder.customerLocation.longitude],
                ...markerPalette.customer
            });
        }

        return markers;
    }, [activeOrder, courierPosition, deliveryStage]);

    const handleAcceptOrder = async (order) => {
        try {
            setActionLoading(order.id);
            await deliverOrder(order.id, courierId);
            setCourierDeliveryStage(order.id, "pickup");

            setAvailableOrders((current) => current.filter((item) => item.id !== order.id));
            setActiveOrders([order]);
        } catch (error) {
            console.error("Failed to accept order", error);
        } finally {
            setActionLoading("");
        }
    };

    const handlePickedUp = async () => {
        if (!activeOrder) return;

        try {
            setActionLoading(activeOrder.id);

            await changeOrderStatus(activeOrder.id, STAGE_TO_STATUS.dropoff);

            // ✅ перемикаємо deliveryStage
            setCourierDeliveryStage(activeOrder.id, "dropoff");

            // ✅ очищаємо попередній маршрут – це критично
            setRoute(null);

            // ✅ оновлюємо activeOrder
            setActiveOrders((current) =>
                current.map((order) =>
                    order.id === activeOrder.id
                        ? { ...order, orderStatus: "outfordelivery" }
                        : order
                )
            );
        } catch (error) {
            console.error("Failed to update pickup status", error);
        } finally {
            setActionLoading("");
        }
    };

    const handleDelivered = async () => {
        if (!activeOrder) {
            return;
        }

        try {
            setActionLoading(activeOrder.id);
            await changeOrderStatus(activeOrder.id, "Delivered");
            clearCourierDeliveryStage(activeOrder.id);
            setHistory((current) => [{ ...activeOrder, orderStatus: "delivered" }, ...current]);
            setActiveOrders([]);
            setRoute(null);
        } catch (error) {
            console.error("Failed to finish delivery", error);
        } finally {
            setActionLoading("");
        }
    };

    const routeSummary = route
        ? `${formatRouteDistance(route.distanceMeters)} | ${formatRouteDuration(route.durationSeconds)}`
        : "Route will appear once coordinates are available";

    return (
        <div className="courier-orders-shell">
            <CourierSidebar
                isOnline={isOnline}
                setIsOnline={setIsOnline}
                userData={{ name: courierName }}
                availableCount={availableOrders.length}
                activeCount={activeOrders.length}
            />

            <main className="courier-orders-page">
                <section className="courier-orders-hero">
                    <div>
                        <span className="courier-orders-eyebrow">Courier orders</span>
                        <h1>Choose the nearest ready order and deliver it in two steps.</h1>
                        <p>
                            The first route leads you to the restaurant. After pickup, the map switches
                            to the customer leg automatically.
                        </p>
                    </div>

                    <div className="courier-orders-hero__chips">
                        <div className="courier-chip">
                            <Package size={16} />
                            {availableOrders.length} ready orders
                        </div>
                        <div className="courier-chip">
                            <Bike size={16} />
                            {activeOrders.length} active delivery
                        </div>
                        <div className="courier-chip">
                            <LocateFixed size={16} />
                            {locationError || "Location synced"}
                        </div>
                    </div>
                </section>

                <section className="courier-orders-layout">
                    <div className="courier-orders-column">
                        <div className="courier-panel">
                            <div className="courier-panel__header">
                                <div>
                                    <span>Available now</span>
                                    <h2>Nearest ready orders</h2>
                                </div>
                            </div>

                            {loading ? (
                                <div className="courier-empty">Loading orders...</div>
                            ) : !isOnline ? (
                                <div className="courier-empty">Switch to online mode to accept deliveries.</div>
                            ) : sortedAvailableOrders.length === 0 ? (
                                <div className="courier-empty">No ready orders nearby right now.</div>
                            ) : (
                                <div className="courier-orders-list">
                                    {sortedAvailableOrders.map((order) => (
                                        <article key={order.id} className="courier-order-card">
                                            <div className="courier-order-card__top">
                                                <div>
                                                    <h3>{order.businessName}</h3>
                                                    <p>Order #{String(order.id).slice(0, 8)}</p>
                                                </div>
                                                <strong>{formatMoney(order.totalPrice)}</strong>
                                            </div>

                                            <div className="courier-order-card__meta">
                                                <div>
                                                    <Store size={14} />
                                                    {order.businessAddress || "Restaurant address unavailable"}
                                                </div>
                                                <div>
                                                    <MapPin size={14} />
                                                    {order.customerAddress || "Customer address unavailable"}
                                                </div>
                                                <div>
                                                    <Clock3 size={14} />
                                                    {new Date(order.orderDate).toLocaleString("uk-UA", {
                                                        hour: "2-digit",
                                                        minute: "2-digit",
                                                        day: "2-digit",
                                                        month: "2-digit"
                                                    })}
                                                </div>
                                                <div>
                                                    <Route size={14} />
                                                    {order.distanceToBusinessKm != null
                                                        ? `${order.distanceToBusinessKm.toFixed(1)} km to restaurant`
                                                        : "Allow geolocation to rank by distance"}
                                                </div>
                                            </div>

                                            <div className="courier-order-card__footer">
                                                <span>Courier fee: {formatMoney(order.profit)}</span>
                                                <button
                                                    type="button"
                                                    className="courier-primary-btn"
                                                    onClick={() => handleAcceptOrder(order)}
                                                    disabled={Boolean(activeOrder) || actionLoading === order.id}
                                                >
                                                    {actionLoading === order.id ? "Assigning..." : "Deliver this order"}
                                                </button>
                                            </div>
                                        </article>
                                    ))}
                                </div>
                            )}
                        </div>

                        <div className="courier-panel courier-panel--compact">
                            <div className="courier-panel__header">
                                <div>
                                    <span>Recent history</span>
                                    <h2>Finished deliveries</h2>
                                </div>
                            </div>

                            {historyPreview.length === 0 ? (
                                <div className="courier-empty">Completed deliveries will appear here.</div>
                            ) : (
                                <div className="courier-history-list">
                                    {historyPreview.map((order) => (
                                        <div key={order.id} className="courier-history-item">
                                            <div>
                                                <strong>{order.businessName}</strong>
                                                <span>{formatMoney(order.totalPrice)}</span>
                                            </div>
                                            <span>{new Date(order.orderDate).toLocaleDateString("uk-UA")}</span>
                                        </div>
                                    ))}
                                </div>
                            )}
                        </div>
                    </div>

                    <div className="courier-orders-column">
                        <div className="courier-panel courier-panel--sticky">
                            <div className="courier-panel__header">
                                <div>
                                    <span>Active delivery</span>
                                    <h2>{activeOrder ? activeOrder.businessName : "No order selected"}</h2>
                                </div>
                                {activeOrder && (
                                    <span className={`courier-stage-badge is-${deliveryStage}`}>
                                        {deliveryStage === "pickup" ? "To restaurant" : "To customer"}
                                    </span>
                                )}
                            </div>

                            {!activeOrder ? (
                                <div className="courier-empty">
                                    Choose any order from the left column to start the route.
                                </div>
                            ) : (
                                <>
                                    <div className="courier-active-summary">
                                        <div>
                                            <span>Current leg</span>
                                            <strong>
                                                {deliveryStage === "pickup"
                                                    ? "Courier -> Restaurant"
                                                    : "Restaurant -> Customer"}
                                            </strong>
                                        </div>
                                        <div>
                                            <span>Road route</span>
                                            <strong>{routeSummary}</strong>
                                        </div>
                                    </div>

                                    <div className="courier-active-stops">
                                        <div className={`courier-stop ${deliveryStage === "pickup" ? "is-active" : "is-complete"}`}>
                                            <div className="courier-stop__icon">
                                                <Store size={18} />
                                            </div>
                                            <div>
                                                <strong>{activeOrder.businessName}</strong>
                                                <p>{activeOrder.businessAddress || "Restaurant address unavailable"}</p>
                                            </div>
                                        </div>

                                        <div className={`courier-stop ${deliveryStage === "dropoff" ? "is-active" : ""}`}>
                                            <div className="courier-stop__icon">
                                                <Navigation size={18} />
                                            </div>
                                            <div>
                                                <strong>Customer destination</strong>
                                                <p>{activeOrder.customerAddress || "Customer address unavailable"}</p>
                                            </div>
                                        </div>
                                    </div>

                                    <div className="courier-map-shell">
                                        <CourierRouteMap
                                            center={routeMarkers[0]?.position ?? [50.4501, 30.5234]}
                                            markers={routeMarkers}
                                            route={route}
                                        />
                                    </div>

                                    <div className="courier-active-actions">
                                        {deliveryStage === "pickup" ? (
                                            <button
                                                type="button"
                                                className="courier-primary-btn"
                                                onClick={handlePickedUp}
                                                disabled={actionLoading === activeOrder.id}
                                            >
                                                {actionLoading === activeOrder.id ? "Updating..." : "Order picked up"}
                                            </button>
                                        ) : (
                                            <button
                                                type="button"
                                                className="courier-primary-btn courier-primary-btn--success"
                                                onClick={handleDelivered}
                                                disabled={actionLoading === activeOrder.id}
                                            >
                                                {actionLoading === activeOrder.id ? "Finishing..." : "Delivered to customer"}
                                            </button>
                                        )}
                                    </div>
                                </>
                            )}
                        </div>
                    </div>
                </section>
            </main>
        </div>
    );
}