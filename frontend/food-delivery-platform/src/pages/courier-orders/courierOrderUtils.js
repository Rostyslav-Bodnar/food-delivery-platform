import { buildLocation, formatLocation, hasCoordinates } from "../../utils/orderLocations.js";

const STAGE_STORAGE_KEY = "courier_delivery_stages";

export const mapCourierOrder = (order) => {
    const businessLocation = buildLocation(order, "business");
    const customerLocation = buildLocation(order, "customer");
    const courierLocation = buildLocation(order, "courier");
    const orderStatus = String(order.orderStatus ?? order.OrderStatus ?? "").toLowerCase();

    return {
        ...order,
        orderStatus,
        businessLocation,
        customerLocation,
        courierLocation,
        businessAddress: formatLocation(businessLocation),
        customerAddress: formatLocation(customerLocation),
        profit: Number(order.profit ?? 0)
    };
};

export const formatMoney = (value) =>
    new Intl.NumberFormat("uk-UA", {
        style: "currency",
        currency: "UAH",
        maximumFractionDigits: 0
    }).format(Number(value ?? 0));

export const formatRouteDistance = (distanceMeters) => {
    if (!distanceMeters) {
        return "Distance unavailable";
    }

    if (distanceMeters < 1000) {
        return `${Math.round(distanceMeters)} m`;
    }

    return `${(distanceMeters / 1000).toFixed(1)} km`;
};

export const formatRouteDuration = (durationSeconds) => {
    if (!durationSeconds) {
        return "ETA unavailable";
    }

    const totalMinutes = Math.round(durationSeconds / 60);

    if (totalMinutes < 60) {
        return `${totalMinutes} min`;
    }

    const hours = Math.floor(totalMinutes / 60);
    const minutes = totalMinutes % 60;

    return minutes > 0 ? `${hours} h ${minutes} min` : `${hours} h`;
};

export const calculateDistanceKm = (start, end) => {
    if (!hasCoordinates(start) || !hasCoordinates(end)) {
        return null;
    }

    const toRadians = (value) => (value * Math.PI) / 180;
    const earthRadiusKm = 6371;
    const deltaLat = toRadians(end.latitude - start.latitude);
    const deltaLng = toRadians(end.longitude - start.longitude);

    const a =
        Math.sin(deltaLat / 2) * Math.sin(deltaLat / 2) +
        Math.cos(toRadians(start.latitude)) *
            Math.cos(toRadians(end.latitude)) *
            Math.sin(deltaLng / 2) *
            Math.sin(deltaLng / 2);

    return earthRadiusKm * 2 * Math.atan2(Math.sqrt(a), Math.sqrt(1 - a));
};

const normalizeStatusKey = (value) =>
    String(value ?? "")
        .trim()
        .toLowerCase()
        .replace(/[^a-z]/g, "");

const readStageMap = () => {
    try {
        return JSON.parse(localStorage.getItem(STAGE_STORAGE_KEY) ?? "{}");
    } catch {
        return {};
    }
};

export const getCourierDeliveryStage = (orderId, orderStatus, liveStage) => {
    const normalizedLiveStage = String(liveStage ?? "").trim().toLowerCase();

    if (normalizedLiveStage === "to-customer") {
        return "dropoff";
    }

    if (normalizedLiveStage === "to-restaurant") {
        return "pickup";
    }

    const normalizedStatus = normalizeStatusKey(orderStatus);

    if (normalizedStatus === "pickedup" || normalizedStatus === "delivered") {
        return "dropoff";
    }

    if (["outfordelivery", "ready", "preparing"].includes(normalizedStatus)) {
        return "pickup";
    }

    const stages = readStageMap();

    if (stages[orderId]) {
        return stages[orderId];
    }

    return "pickup";
};

export const setCourierDeliveryStage = (orderId, stage) => {
    const stages = readStageMap();
    stages[orderId] = stage;
    localStorage.setItem(STAGE_STORAGE_KEY, JSON.stringify(stages));
};

export const clearCourierDeliveryStage = (orderId) => {
    const stages = readStageMap();
    delete stages[orderId];
    localStorage.setItem(STAGE_STORAGE_KEY, JSON.stringify(stages));
};
