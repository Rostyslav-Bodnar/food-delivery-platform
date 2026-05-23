import React from "react";
import {
    CircleMarker,
    MapContainer,
    Polyline,
    TileLayer,
    Tooltip,
    useMap
} from "react-leaflet";
import "leaflet/dist/leaflet.css";
import {
    Bike,
    MapPin,
    PackageSearch,
    Route,
    Store,
    X
} from "lucide-react";

const MapViewUpdater = ({ bounds }) => {
    const map = useMap();

    React.useEffect(() => {
        if (bounds.length >= 2) {
            map.fitBounds(bounds, {
                padding: [40, 40]
            });
        } else if (bounds.length === 1) {
            map.setView(bounds[0], 14, {
                animate: true
            });
        }
    }, [bounds, map]);

    return null;
};

const statusSteps = {
    new: "Order created",
    preparing: "Preparing",
    "on-the-way": "Courier route",
    delivered: "Delivered",
    cancelled: "Cancelled"
};

const formatDistance = (distanceMeters) => {
    if (!distanceMeters) {
        return null;
    }

    if (distanceMeters < 1000) {
        return `${Math.round(distanceMeters)} m`;
    }

    return `${(distanceMeters / 1000).toFixed(1)} km`;
};

const formatDuration = (durationSeconds) => {
    if (!durationSeconds) {
        return null;
    }

    const totalMinutes = Math.round(durationSeconds / 60);
    if (totalMinutes < 60) {
        return `${totalMinutes} min`;
    }

    const hours = Math.floor(totalMinutes / 60);
    const minutes = totalMinutes % 60;
    return minutes > 0 ? `${hours} h ${minutes} min` : `${hours} h`;
};

export default function OrderTrackingModal({
    order,
    tracking,
    onClose
}) {
    if (!order || !tracking) {
        return null;
    }

    const markers = [
        tracking.restaurantCoords ? {
            key: "restaurant",
            label: tracking.restaurantAddress,
            position: [tracking.restaurantCoords.latitude, tracking.restaurantCoords.longitude]
        } : null,
        tracking.customerCoords ? {
            key: "customer",
            label: tracking.customerAddress,
            position: [tracking.customerCoords.latitude, tracking.customerCoords.longitude]
        } : null
    ].filter(Boolean);

    const routeCoordinates = tracking.route?.coordinates ?? [];
    const hasRoute = routeCoordinates.length > 1;
    const distanceLabel = formatDistance(tracking.route?.distanceMeters);
    const durationLabel = formatDuration(tracking.route?.durationSeconds);

    return (
        <div className="customer-orders-modal-overlay" onClick={onClose}>
            <div className="customer-orders-modal customer-orders-modal--wide" onClick={(event) => event.stopPropagation()}>
                <button
                    type="button"
                    className="customer-orders-modal__close"
                    onClick={onClose}
                    aria-label="Close tracking dialog"
                >
                    <X size={18} />
                </button>

                <div className="tracking-modal__header">
                    <div>
                        <span className="tracking-modal__eyebrow">
                            <PackageSearch size={14} />
                            Order route overview
                        </span>
                        <h3>{order.restaurant}</h3>
                        <p>
                            This map shows the restaurant and delivery destination for order #{order.id.slice(0, 8)}.
                        </p>
                    </div>

                    <div className="tracking-modal__status-chip">
                        <Bike size={14} />
                        {statusSteps[order.status] ?? "Order active"}
                    </div>
                </div>

                <div className="tracking-modal__layout">
                    <div className="tracking-modal__map-shell">
                        <MapContainer
                            center={markers[0]?.position ?? [50.4501, 30.5234]}
                            zoom={13}
                            scrollWheelZoom={false}
                            className="tracking-modal__map"
                        >
                            <MapViewUpdater bounds={markers.map((marker) => marker.position)} />
                            <TileLayer
                                url="https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png"
                                attribution="&copy; OpenStreetMap contributors"
                            />

                            {hasRoute && (
                                <Polyline
                                    positions={routeCoordinates}
                                    pathOptions={{
                                        color: "#7c5cff",
                                        weight: 5,
                                        opacity: 0.85
                                    }}
                                />
                            )}

                            {markers.map((marker) => (
                                <CircleMarker
                                    key={marker.key}
                                    center={marker.position}
                                    radius={11}
                                    pathOptions={{
                                        color: marker.key === "restaurant" ? "#00d4ff" : "#ffb86b",
                                        fillColor: marker.key === "restaurant" ? "#00d4ff" : "#ff6b6b",
                                        fillOpacity: 0.92,
                                        weight: 3
                                    }}
                                >
                                    <Tooltip direction="top" offset={[0, -10]} opacity={1}>
                                        {marker.label}
                                    </Tooltip>
                                </CircleMarker>
                            ))}
                        </MapContainer>
                    </div>

                    <div className="tracking-modal__sidebar">
                        <div className="tracking-modal__info-card">
                            <span className="tracking-modal__label">
                                <Store size={14} />
                                Restaurant
                            </span>
                            <strong>{tracking.restaurantAddress}</strong>
                        </div>

                        <div className="tracking-modal__info-card">
                            <span className="tracking-modal__label">
                                <MapPin size={14} />
                                Delivery address
                            </span>
                            <strong>{tracking.customerAddress}</strong>
                        </div>

                        {(distanceLabel || durationLabel) && (
                            <div className="tracking-modal__info-card">
                                <span className="tracking-modal__label">
                                    <Route size={14} />
                                    Road route
                                </span>
                                <strong>
                                    {[distanceLabel, durationLabel].filter(Boolean).join(" | ")}
                                </strong>
                            </div>
                        )}

                        <div className="tracking-modal__info-card">
                            <span className="tracking-modal__label">
                                <Route size={14} />
                                Status flow
                            </span>
                            <ul className="tracking-modal__steps">
                                <li className="is-active">Order created</li>
                                <li className={["preparing", "on-the-way", "delivered"].includes(order.status) ? "is-active" : ""}>Preparing</li>
                                <li className={["on-the-way", "delivered"].includes(order.status) ? "is-active" : ""}>Courier route</li>
                                <li className={order.status === "delivered" ? "is-active" : ""}>Delivered</li>
                            </ul>
                        </div>

                        {order.courier?.name && (
                            <div className="tracking-modal__info-card">
                                <span className="tracking-modal__label">
                                    <Bike size={14} />
                                    Courier
                                </span>
                                <strong>{order.courier.name}</strong>
                            </div>
                        )}
                    </div>
                </div>
            </div>
        </div>
    );
}
