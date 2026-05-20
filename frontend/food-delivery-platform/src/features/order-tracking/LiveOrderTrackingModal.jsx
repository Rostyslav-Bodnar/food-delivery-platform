import React from "react";
import {
    CircleMarker,
    GeoJSON,
    MapContainer,
    TileLayer,
    Tooltip,
    useMap
} from "react-leaflet";

import "leaflet/dist/leaflet.css";

import {
    Bike,
    Clock3,
    MapPin,
    Navigation,
    PackageSearch,
    Route,
    Signal,
    Store,
    X
} from "lucide-react";

import useCourierTracking from "../../hooks/useCourierTracking.jsx";

import {
    formatLocation,
    hasCoordinates
} from "../../utils/orderLocations.js";

import { getRoadRoute } from "../../utils/roadRouting.js";

import {
    getTrackingStageMeta,
    resolveTrackingStage,
    TRACKING_STAGE
} from "./trackingStages.js";

import "./LiveOrderTrackingModal.css";

const DEFAULT_CENTER = [50.4501, 30.5234];

const MapViewport = ({ bounds }) => {
    const map = useMap();

    React.useEffect(() => {
        if (bounds.length >= 2) {
            map.fitBounds(bounds, {
                padding: [36, 36],
                animate: true
            });
        } else if (bounds.length === 1) {
            map.setView(bounds[0], 14, {
                animate: true
            });
        }
    }, [bounds, map]);

    return null;
};

const formatDistance = (distanceMeters) => {
    if (!distanceMeters) {
        return "Distance unavailable";
    }

    if (distanceMeters < 1000) {
        return `${Math.round(distanceMeters)} m`;
    }

    return `${(distanceMeters / 1000).toFixed(1)} km`;
};

const formatDuration = (durationSeconds) => {
    if (!durationSeconds) {
        return "ETA unavailable";
    }

    const totalMinutes = Math.round(durationSeconds / 60);

    if (totalMinutes < 60) {
        return `${totalMinutes} min`;
    }

    const hours = Math.floor(totalMinutes / 60);
    const minutes = totalMinutes % 60;

    return minutes > 0
        ? `${hours} h ${minutes} min`
        : `${hours} h`;
};

const formatUpdatedAt = (value) => {
    if (!value) {
        return "No live update yet";
    }

    const parsed = new Date(value);

    if (Number.isNaN(parsed.getTime())) {
        return "No live update yet";
    }

    return parsed.toLocaleTimeString("uk-UA", {
        hour: "2-digit",
        minute: "2-digit",
        second: "2-digit"
    });
};

const toMarker = (key, label, location, palette) => ({
    key,
    label,
    position: [location.latitude, location.longitude],
    ...palette
});

const calculateDistanceMeters = (start, end) => {
    if (!start || !end) {
        return Number.POSITIVE_INFINITY;
    }

    const toRadians = (value) => (value * Math.PI) / 180;

    const earthRadiusMeters = 6371000;

    const deltaLat = toRadians(end.latitude - start.latitude);
    const deltaLng = toRadians(end.longitude - start.longitude);

    const a =
        Math.sin(deltaLat / 2) * Math.sin(deltaLat / 2) +
        Math.cos(toRadians(start.latitude)) *
        Math.cos(toRadians(end.latitude)) *
        Math.sin(deltaLng / 2) *
        Math.sin(deltaLng / 2);

    return earthRadiusMeters * 2 * Math.atan2(
        Math.sqrt(a),
        Math.sqrt(1 - a)
    );
};

const getConnectionLabel = (status) => {
    switch (status) {
        case "connected":
            return "Live updates connected";

        case "connecting":
            return "Connecting to live updates";

        case "offline":
            return "Live updates paused";

        case "error":
            return "Live updates unavailable";

        default:
            return "Waiting for live session";
    }
};

export default function LiveOrderTrackingModal({
                                                   order,
                                                   onClose
                                               }) {

    const { snapshot, status } = useCourierTracking(order?.id, {
        enabled: Boolean(order?.id)
    });

    const [route, setRoute] = React.useState(null);

    const lastRouteRef = React.useRef({
        stage: null,
        destinationKey: null,
        courierLocation: null,
        builtAt: 0
    });

    React.useEffect(() => {
        document.body.style.overflow = "hidden";

        return () => {
            document.body.style.overflow = "";
        };
    }, []);

    const businessLocation = order?.businessLocation;

    const customerLocation = order?.customerLocation;

    const businessAddress =
        formatLocation(businessLocation) ||
        order?.businessAddress ||
        order?.address ||
        "Restaurant address unavailable";

    const customerAddress =
        formatLocation(customerLocation) ||
        order?.customerAddress ||
        "Customer address unavailable";

    const courierLocation =
        snapshot?.courierLocation &&
        hasCoordinates(snapshot.courierLocation)
            ? snapshot.courierLocation
            : hasCoordinates(order?.courierLocation)
                ? order.courierLocation
                : null;

    const effectiveOrderStatus =
        snapshot?.orderStatus ??
        order?.rawStatus ??
        order?.orderStatus;

    const trackingStage = resolveTrackingStage(
        effectiveOrderStatus,
        snapshot?.stage
    );

    const trackingMeta = getTrackingStageMeta(trackingStage);

    const routeDestination =
        trackingStage === TRACKING_STAGE.toCustomer
            ? customerLocation
            : businessLocation;

    React.useEffect(() => {

        if (
            !courierLocation ||
            !hasCoordinates(routeDestination) ||
            ![
                TRACKING_STAGE.toRestaurant,
                TRACKING_STAGE.toCustomer
            ].includes(trackingStage)
        ) {
            setRoute(null);
            return undefined;
        }

        const destinationKey =
            `${routeDestination.latitude}:${routeDestination.longitude}`;

        const movedMeters = calculateDistanceMeters(
            lastRouteRef.current.courierLocation,
            courierLocation
        );

        const now = Date.now();

        const shouldReusePreviousRoute =
            lastRouteRef.current.stage === trackingStage &&
            lastRouteRef.current.destinationKey === destinationKey &&
            movedMeters < 75 &&
            now - lastRouteRef.current.builtAt < 15000;

        if (shouldReusePreviousRoute) {
            return undefined;
        }

        let cancelled = false;

        const buildRoute = async () => {
            try {

                const nextRoute = await getRoadRoute(
                    courierLocation,
                    routeDestination
                );

                if (cancelled) {
                    return;
                }

                lastRouteRef.current = {
                    stage: trackingStage,
                    destinationKey,
                    courierLocation,
                    builtAt: Date.now()
                };

                setRoute(nextRoute);

            } catch (error) {

                console.error(
                    "Failed to build live tracking route",
                    error
                );

                if (!cancelled) {
                    setRoute(null);
                }
            }
        };

        buildRoute();

        return () => {
            cancelled = true;
        };

    }, [
        courierLocation,
        routeDestination,
        trackingStage
    ]);

    if (!order) {
        return null;
    }

    const markers = [

        hasCoordinates(businessLocation)
            ? toMarker(
                "restaurant",
                businessAddress,
                businessLocation,
                {
                    fillColor: "#ffb86b",
                    strokeColor: "#ffd8a6"
                }
            )
            : null,

        hasCoordinates(customerLocation)
            ? toMarker(
                "customer",
                customerAddress,
                customerLocation,
                {
                    fillColor: "#00d4ff",
                    strokeColor: "#bff6ff"
                }
            )
            : null,

        courierLocation
            ? toMarker(
                "courier",
                "Courier live location",
                courierLocation,
                {
                    fillColor: "#7c5cff",
                    strokeColor: "#ddd3ff"
                }
            )
            : null

    ].filter(Boolean);

    const routeGeoJson =
        route?.coordinates?.length > 1
            ? {
                type: "Feature",

                geometry: {
                    type: "LineString",

                    coordinates: route.coordinates.map(
                        ([latitude, longitude]) => [
                            longitude,
                            latitude
                        ]
                    )
                },

                properties: {}
            }
            : null;

    const routeLabel = route
        ? `${formatDistance(route.distanceMeters)} | ${formatDuration(route.durationSeconds)}`
        : courierLocation
            ? "Route updates when the courier changes position"
            : "Waiting for the courier app to publish live location";

    const orderTitle =
        order.restaurant ??
        order.businessName ??
        "Live order tracking";

    const preparingActive =
        ![TRACKING_STAGE.cancelled].includes(trackingStage) &&
        String(effectiveOrderStatus ?? "").toLowerCase() !== "new";

    const pickupActive = [
        TRACKING_STAGE.toRestaurant,
        TRACKING_STAGE.toCustomer,
        TRACKING_STAGE.delivered
    ].includes(trackingStage);

    const pickedUpActive = [
        TRACKING_STAGE.toCustomer,
        TRACKING_STAGE.delivered
    ].includes(trackingStage);

    const dropoffActive = [
        TRACKING_STAGE.toCustomer,
        TRACKING_STAGE.delivered
    ].includes(trackingStage);

    const deliveredActive =
        trackingStage === TRACKING_STAGE.delivered;

    return (
        <div
            className="tracking-live-modal__overlay"
            onClick={onClose}
        >

            <div
                className="tracking-live-modal"
                onClick={(event) => event.stopPropagation()}
                onWheel={(event) => event.stopPropagation()}
            >

                <button
                    type="button"
                    className="tracking-live-modal__close"
                    onClick={onClose}
                    aria-label="Close live tracking"
                >
                    <X size={18} />
                </button>

                <div className="tracking-live-modal__header">

                    <div>

                        <span className="tracking-live-modal__eyebrow">
                            <PackageSearch size={14} />
                            Live order tracking
                        </span>

                        <h3>
                            {orderTitle}
                        </h3>

                        <p>
                            Order #
                            {String(order.id).slice(0, 8)}.
                            {" "}
                            {trackingMeta.description}
                        </p>

                    </div>

                    <div className="tracking-live-modal__header-chips">

                        <div className="tracking-live-modal__chip">
                            <Navigation size={14} />
                            {trackingMeta.shortLabel}
                        </div>

                        <div className="tracking-live-modal__chip tracking-live-modal__chip--muted">
                            <Signal size={14} />
                            {getConnectionLabel(status)}
                        </div>

                    </div>

                </div>

                <div className="tracking-live-modal__content">

                    <div className="tracking-live-modal__layout">

                        <div className="tracking-live-modal__map-shell">

                            <MapContainer
                                center={markers[0]?.position ?? DEFAULT_CENTER}
                                zoom={13}
                                scrollWheelZoom={true}
                                doubleClickZoom={true}
                                touchZoom={true}
                                dragging={true}
                                zoomControl={true}
                                attributionControl={false}
                                className="tracking-live-modal__map"
                            >

                                <MapViewport
                                    bounds={markers.map(
                                        (marker) => marker.position
                                    )}
                                />

                                <TileLayer
                                    url="https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png"
                                    attribution="&copy; OpenStreetMap contributors"
                                />

                                {routeGeoJson && (
                                    <GeoJSON
                                        data={routeGeoJson}
                                        style={{
                                            color: "#7c5cff",
                                            weight: 5,
                                            opacity: 0.86
                                        }}
                                    />
                                )}

                                {markers.map((marker) => (

                                    <CircleMarker
                                        key={marker.key}
                                        center={marker.position}
                                        radius={10}
                                        pathOptions={{
                                            color: marker.strokeColor,
                                            fillColor: marker.fillColor,
                                            fillOpacity: 0.92,
                                            weight: 3
                                        }}
                                    >

                                        <Tooltip
                                            direction="top"
                                            offset={[0, -10]}
                                            opacity={1}
                                        >
                                            {marker.label}
                                        </Tooltip>

                                    </CircleMarker>

                                ))}

                            </MapContainer>

                        </div>

                        <div className="tracking-live-modal__sidebar">

                            <div className="tracking-live-modal__card">

                                <span className="tracking-live-modal__label">
                                    <Route size={14} />
                                    Current leg
                                </span>

                                <strong>
                                    {trackingMeta.title}
                                </strong>

                                <p>
                                    {routeLabel}
                                </p>

                            </div>

                            <div className="tracking-live-modal__card">

                                <span className="tracking-live-modal__label">
                                    <Store size={14} />
                                    Restaurant
                                </span>

                                <strong>
                                    {businessAddress}
                                </strong>

                            </div>

                            <div className="tracking-live-modal__card">

                                <span className="tracking-live-modal__label">
                                    <MapPin size={14} />
                                    Customer
                                </span>

                                <strong>
                                    {customerAddress}
                                </strong>

                            </div>

                            <div className="tracking-live-modal__card">

                                <span className="tracking-live-modal__label">
                                    <Bike size={14} />
                                    Courier
                                </span>

                                <strong>
                                    {order.courier?.name ??
                                        "Courier not assigned yet"}
                                </strong>

                                <p>
                                    Last update:
                                    {" "}
                                    {formatUpdatedAt(snapshot?.updatedAtUtc)}
                                </p>

                            </div>

                            <div className="tracking-live-modal__card">

                                <span className="tracking-live-modal__label">
                                    <Clock3 size={14} />
                                    Progress
                                </span>

                                <ul className="tracking-live-modal__steps">

                                    <li className={preparingActive ? "is-active" : ""}>
                                        Order in progress
                                    </li>

                                    <li className={pickupActive ? "is-active" : ""}>
                                        Courier to restaurant
                                    </li>

                                    <li className={pickedUpActive ? "is-active" : ""}>
                                        Picked up
                                    </li>

                                    <li className={dropoffActive ? "is-active" : ""}>
                                        Courier to customer
                                    </li>

                                    <li className={deliveredActive ? "is-active" : ""}>
                                        Delivered
                                    </li>

                                </ul>

                            </div>

                        </div>

                    </div>

                </div>

            </div>

        </div>
    );
}