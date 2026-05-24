const trimTrailingSlash = (value) => value.replace(/\/+$/, "");

// TrackingService origin for SignalR hub negotiation. Defaults to the
// deployed Render instance; override with `VITE_TRACKING_API_ORIGIN` in
// `.env.local` (e.g. `http://localhost:5006`) when running the service
// locally.
const trackingApiOrigin = trimTrailingSlash(
    import.meta.env.VITE_TRACKING_API_ORIGIN ?? "https://trackingservice-5ray.onrender.com"
);

export const TRACKING_HUB_URL = `${trackingApiOrigin}/hubs/courier-tracking`;
