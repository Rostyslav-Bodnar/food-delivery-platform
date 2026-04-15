import { useEffect, useRef, useState } from "react";
import * as signalR from "@microsoft/signalr";

const HUB_URL = "http://localhost:5006/hubs/courier-tracking";

export default function useCourierTracking(orderId) {
    const [location, setLocation] = useState(null);
    const [status, setStatus] = useState("idle");
    // idle | connecting | connected | offline | error

    const connectionRef = useRef(null);
    const offlineTimerRef = useRef(null);

    useEffect(() => {
        if (!orderId) return;

        let cancelled = false;

        const start = async () => {
            try {
                setStatus("connecting");

                const accessToken = localStorage.getItem("accessToken");
                if (!accessToken) throw new Error("No access token");

                const res = await fetch(`/api/orders/${orderId}/tracking-token`, {
                    method: "POST",
                    headers: {
                        Authorization: `Bearer ${accessToken}`
                    }
                });

                if (!res.ok) throw new Error("Failed to fetch tracking token");

                const { token } = await res.json();
                if (cancelled) return;

                const connection = new signalR.HubConnectionBuilder()
                    .withUrl(HUB_URL, {
                        accessTokenFactory: () => token
                    })
                    .withAutomaticReconnect([0, 2000, 5000, 10000])
                    .configureLogging(signalR.LogLevel.Warning)
                    .build();

                connection.on("CourierLocationUpdated", (dto) => {
                    setLocation(dto);
                    setStatus("connected");

                    if (offlineTimerRef.current) {
                        clearTimeout(offlineTimerRef.current);
                    }

                    offlineTimerRef.current = setTimeout(() => {
                        setStatus("offline");
                    }, 60000);
                });

                connection.onreconnecting(() => setStatus("connecting"));
                connection.onreconnected(() => setStatus("connected"));
                connection.onclose(() => setStatus("offline"));

                await connection.start();
                if (cancelled) return;

                await connection.invoke("SubscribeToOrder", orderId);

                connectionRef.current = connection;
                setStatus("connected");
            } catch (err) {
                console.error("Tracking error:", err);
                setStatus("error");
            }
        };

        start();

        return () => {
            cancelled = true;

            if (offlineTimerRef.current) {
                clearTimeout(offlineTimerRef.current);
            }

            if (connectionRef.current) {
                connectionRef.current.stop();
                connectionRef.current = null;
            }
        };
    }, [orderId]);

    return { location, status };
}