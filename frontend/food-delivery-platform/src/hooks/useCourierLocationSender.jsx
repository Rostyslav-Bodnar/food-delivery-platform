import { useEffect, useRef } from "react";
import * as signalR from "@microsoft/signalr";

const HUB_URL = "http://localhost:5006/hubs/courier-tracking";

export default function useCourierLocationSender({
                                                     orderId,
                                                     courierId,
                                                     position,
                                                     enabled = true
                                                 }) {
    const connectionRef = useRef(null);
    const intervalRef = useRef(null);

    useEffect(() => {
        if (!enabled || !orderId || !courierId || !position) return;

        let cancelled = false;

        const start = async () => {
            const accessToken = localStorage.getItem("accessToken");
            if (!accessToken) return;

            const res = await fetch(`/api/orders/${orderId}/tracking-token`, {
                method: "POST",
                headers: {
                    Authorization: `Bearer ${accessToken}`
                }
            });

            if (!res.ok) return;

            const { token } = await res.json();
            if (cancelled) return;

            const connection = new signalR.HubConnectionBuilder()
                .withUrl(HUB_URL, {
                    accessTokenFactory: () => token
                })
                .withAutomaticReconnect()
                .build();

            await connection.start();
            connectionRef.current = connection;

            intervalRef.current = setInterval(() => {
                connection.invoke("SendLocation", {
                    orderId,
                    courierId,
                    latitude: position.latitude,
                    longitude: position.longitude,
                    speed: null,
                    heading: null,
                    timestampUtc: new Date().toISOString()
                }).catch(() => {});
            }, 5000);
        };

        start();

        return () => {
            cancelled = true;

            if (intervalRef.current) {
                clearInterval(intervalRef.current);
                intervalRef.current = null;
            }

            if (connectionRef.current) {
                connectionRef.current.stop();
                connectionRef.current = null;
            }
        };
    }, [orderId, courierId, position, enabled]);
}