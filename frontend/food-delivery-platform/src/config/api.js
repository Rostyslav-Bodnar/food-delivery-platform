const trimTrailingSlash = (value) => value.replace(/\/+$/, "");

const trackingApiOrigin = trimTrailingSlash(
    "http://localhost:5006"
);

export const TRACKING_HUB_URL = `${trackingApiOrigin}/hubs/courier-tracking`;
