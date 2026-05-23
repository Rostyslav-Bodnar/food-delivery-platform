import { useEffect, useRef } from "react";
import * as signalR from "@microsoft/signalr";
import { TRACKING_HUB_URL } from "../config/api.js";

/**
 * Subscribes the current user to live order-status events via the TrackingService
 * SignalR hub. Joins customer:{id} / business:{id} / courier:{id} group based on
 * the logged-in account, and invokes `onStatusChanged` whenever any of the user's
 * orders transition status.
 *
 * @param {object} options
 * @param {boolean} options.enabled - whether the subscription is active.
 * @param {(payload: { orderId, newStatus, previousStatus, businessId, customerId, courierId, changedAtUtc }) => void} options.onStatusChanged
 */
export default function useOrderEventsSubscription({ enabled, onStatusChanged }) {
    const connectionRef = useRef(null);
    const handlerRef = useRef(onStatusChanged);

    // Keep the latest callback without re-running the effect on every render.
    useEffect(() => {
        handlerRef.current = onStatusChanged;
    }, [onStatusChanged]);

    useEffect(() => {
        if (!enabled) return undefined;

        const token = localStorage.getItem("accessToken");
        if (!token) return undefined;

        let cancelled = false;

        const start = async () => {
            try {
                const connection = new signalR.HubConnectionBuilder()
                    .withUrl(TRACKING_HUB_URL, {
                        accessTokenFactory: () => token
                    })
                    .withAutomaticReconnect([0, 2000, 5000, 10000])
                    .configureLogging(signalR.LogLevel.Warning)
                    .build();

                connection.on("OrderStatusChanged", (payload) => {
                    handlerRef.current?.(payload);
                });

                // Re-join the user group on every reconnect — server-side group
                // membership is connection-bound and resets when the socket drops.
                connection.onreconnected(async () => {
                    try {
                        await connection.invoke("SubscribeToUserOrders");
                    } catch (err) {
                        console.warn("Failed to resubscribe after reconnect", err);
                    }
                });

                await connection.start();
                if (cancelled) {
                    await connection.stop();
                    return;
                }

                await connection.invoke("SubscribeToUserOrders");
                connectionRef.current = connection;
            } catch (err) {
                console.error("Order events subscription failed", err);
            }
        };

        start();

        return () => {
            cancelled = true;
            const connection = connectionRef.current;
            connectionRef.current = null;
            if (connection) {
                connection.invoke("UnsubscribeFromUserOrders").catch(() => {});
                connection.stop().catch(() => {});
            }
        };
    }, [enabled]);
}
