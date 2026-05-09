const trimTrailingSlash = (value) => value.replace(/\/+$/, "");

const orderApiOrigin = trimTrailingSlash(
    import.meta.env.VITE_ORDER_API_URL ?? "http://localhost:5229"
);

const trackingApiOrigin = trimTrailingSlash(
    import.meta.env.VITE_TRACKING_API_URL ?? "http://localhost:5185"
);

export const ORDER_API_BASE = `${orderApiOrigin}/api`;
export const TRACKING_API_BASE = `${trackingApiOrigin}/api`;
export const TRACKING_HUB_URL = `${trackingApiOrigin}/hubs/courier-tracking`;
