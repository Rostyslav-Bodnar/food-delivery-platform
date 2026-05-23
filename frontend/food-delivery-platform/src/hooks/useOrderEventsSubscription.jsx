import { useEffect, useRef } from "react";
import * as signalR from "@microsoft/signalr";
import { TRACKING_HUB_URL } from "../config/api.js";

/**
 * Subscribes the current user to live order-status events via the TrackingService
 * SignalR hub. Joins customer:{id} / business:{id} / courier:{id} group (plus
 * couriers:available if the user is a courier), and invokes the matching
 * callback whenever an event arrives.
 *
 * @param {object} options
 * @param {boolean} options.enabled - whether the subscription is active.
 * @param {(payload) => void} [options.onStatusChanged]
 * @param {(payload) => void} [options.onCourierPaid]
 * @param {() => void} [options.onReconnected] - fires after the socket reconnects
 *        so the page can refetch to catch up on missed events.
 */
export default function useOrderEventsSubscription({
    enabled,
    onStatusChanged,
    onCourierPaid,
    onReconnected
}) {
    const connectionRef = useRef(null);
    const statusHandlerRef = useRef(onStatusChanged);
    const paidHandlerRef = useRef(onCourierPaid);
    const reconnectedHandlerRef = useRef(onReconnected);

    // Keep the latest callbacks without re-running the effect on every render.
    useEffect(() => {
        statusHandlerRef.current = onStatusChanged;
        paidHandlerRef.current = onCourierPaid;
        reconnectedHandlerRef.current = onReconnected;
    }, [onStatusChanged, onCourierPaid, onReconnected]);

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
                    statusHandlerRef.current?.(payload);
                });

                connection.on("OrderCourierPaidUpdated", (payload) => {
                    paidHandlerRef.current?.(payload);
                });

                // Re-join the user group on every reconnect — server-side group
                // membership is connection-bound and resets when the socket drops.
                // Also fire onReconnected so the page can refetch and catch up on
                // any events that fired while we were offline.
                connection.onreconnected(async () => {
                    try {
                        await connection.invoke("SubscribeToUserOrders");
                        reconnectedHandlerRef.current?.();
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
