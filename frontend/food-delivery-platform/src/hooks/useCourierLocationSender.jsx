import { useCallback, useEffect, useRef, useState } from "react";
import * as signalR from "@microsoft/signalr";
//import { getTrackingAccessToken } from "../api/Order.jsx";
import { TRACKING_HUB_URL } from "../config/api.js";

export default function useCourierLocationSender({
    orderId,
    courierId,
    position,
    stage,
    enabled = true
}) {
    const [connectionStatus, setConnectionStatus] = useState("idle");

    const connectionRef = useRef(null);
    const intervalRef = useRef(null);
    const latestRef = useRef({
        orderId,
        courierId,
        position,
        stage
    });

    useEffect(() => {
        latestRef.current = {
            orderId,
            courierId,
            position,
            stage
        };
    }, [courierId, orderId, position, stage]);

    const publishStage = useCallback(async (nextStage) => {
        const connection = connectionRef.current;
        const currentOrderId = latestRef.current.orderId;

        if (!connection || connection.state !== signalR.HubConnectionState.Connected || !currentOrderId || !nextStage) {
            return;
        }

        await connection.invoke("UpdateTrackingStage", currentOrderId, nextStage);
    }, []);

    useEffect(() => {
        if (!enabled || !orderId || !courierId) {
            setConnectionStatus("idle");
            return undefined;
        }

        let cancelled = false;

        const sendLocation = async () => {
            const connection = connectionRef.current;
            const current = latestRef.current;

            if (
                !connection ||
                connection.state !== signalR.HubConnectionState.Connected ||
                !current.orderId ||
                !current.courierId ||
                !current.position
            ) {
                return;
            }

            await connection.invoke("SendLocation", {
                orderId: current.orderId,
                courierId: current.courierId,
                latitude: current.position.latitude,
                longitude: current.position.longitude,
                speed: null,
                heading: null,
                timestampUtc: new Date().toISOString()
            });
        };

        const start = async () => {
            try {
                setConnectionStatus("connecting");

                const { token } = "token"//await getTrackingAccessToken(orderId);
                if (cancelled) {
                    return;
                }

                const connection = new signalR.HubConnectionBuilder()
                    .withUrl(TRACKING_HUB_URL, {
                        accessTokenFactory: () => token
                    })
                    .withAutomaticReconnect([0, 2000, 5000, 10000])
                    .configureLogging(signalR.LogLevel.Warning)
                    .build();

                connection.onreconnecting(() => setConnectionStatus("connecting"));
                connection.onreconnected(() => {
                    setConnectionStatus("connected");
                    publishStage(latestRef.current.stage).catch(() => {});
                });
                connection.onclose(() => setConnectionStatus("offline"));

                await connection.start();
                if (cancelled) {
                    await connection.stop();
                    return;
                }

                connectionRef.current = connection;
                setConnectionStatus("connected");

                if (latestRef.current.stage) {
                    await publishStage(latestRef.current.stage);
                }

                await sendLocation();

                intervalRef.current = window.setInterval(() => {
                    sendLocation().catch(() => {});
                }, 5000);
            } catch (error) {
                console.error("Failed to start courier tracking publisher", error);
                setConnectionStatus("error");
            }
        };

        start();

        return () => {
            cancelled = true;

            if (intervalRef.current) {
                window.clearInterval(intervalRef.current);
                intervalRef.current = null;
            }

            const connection = connectionRef.current;
            connectionRef.current = null;

            if (connection) {
                connection.stop().catch(() => {});
            }
        };
    }, [courierId, enabled, orderId, publishStage]);

    useEffect(() => {
        if (!stage) {
            return;
        }

        publishStage(stage).catch(() => {});
    }, [publishStage, stage]);

    return {
        connectionStatus,
        publishStage
    };
}
