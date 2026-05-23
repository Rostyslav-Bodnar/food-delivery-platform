export const TRACKING_STAGE = {
    awaitingCourier: "awaiting-courier",
    toRestaurant: "to-restaurant",
    toCustomer: "to-customer",
    delivered: "delivered",
    cancelled: "cancelled"
};

export const normalizeTrackingStage = (value) => {
    const stage = String(value ?? "").trim().toLowerCase();

    if (Object.values(TRACKING_STAGE).includes(stage)) {
        return stage;
    }

    return TRACKING_STAGE.awaitingCourier;
};

export const resolveTrackingStage = (rawStatus, liveStage) => {
    const normalizedLiveStage = normalizeTrackingStage(liveStage);
    const normalizedRawStatus = String(rawStatus ?? "").trim().toLowerCase();
    const normalizedRawStatusKey = normalizedRawStatus.replace(/[^a-z]/g, "");

    if (normalizedRawStatus === "delivered") {
        return TRACKING_STAGE.delivered;
    }

    if (normalizedRawStatus === "canceled" || normalizedRawStatus === "cancelled") {
        return TRACKING_STAGE.cancelled;
    }

    if (normalizedLiveStage !== TRACKING_STAGE.awaitingCourier) {
        return normalizedLiveStage;
    }

    if (normalizedRawStatusKey === "pickedup") {
        return TRACKING_STAGE.toCustomer;
    }

    if (normalizedRawStatusKey === "outfordelivery") {
        return TRACKING_STAGE.toRestaurant;
    }

    return TRACKING_STAGE.awaitingCourier;
};

export const getTrackingStageMeta = (stage) => {
    switch (stage) {
        case TRACKING_STAGE.toRestaurant:
            return {
                title: "Courier heading to restaurant",
                description: "The first leg ends at the pickup point.",
                shortLabel: "To restaurant"
            };
        case TRACKING_STAGE.toCustomer:
            return {
                title: "Courier heading to customer",
                description: "Pickup is complete and the drop-off leg is live.",
                shortLabel: "To customer"
            };
        case TRACKING_STAGE.delivered:
            return {
                title: "Order delivered",
                description: "The courier finished the route.",
                shortLabel: "Delivered"
            };
        case TRACKING_STAGE.cancelled:
            return {
                title: "Order cancelled",
                description: "Live tracking has stopped for this order.",
                shortLabel: "Cancelled"
            };
        default:
            return {
                title: "Waiting for courier assignment",
                description: "Tracking starts once a courier accepts the order.",
                shortLabel: "Awaiting courier"
            };
    }
};
