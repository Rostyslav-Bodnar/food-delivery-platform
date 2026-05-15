import { useEffect, useRef, useState } from "react";
import * as signalR from "@microsoft/signalr";
//import { getTrackingAccessToken } from "../api/Order.jsx";
import { TRACKING_HUB_URL } from "../config/api.js";

const normalizeLocation = (dto) => {
    if (!dto) {
        return null;
    }

    return {
        orderId: dto.orderId ?? dto.OrderId,
        courierId: dto.courierId ?? dto.CourierId,
        latitude: Number(dto.latitude ?? dto.Latitude ?? 0),
        longitude: Number(dto.longitude ?? dto.Longitude ?? 0),
        speed: dto.speed ?? dto.Speed ?? null,
        heading: dto.heading ?? dto.Heading ?? null,
        timestampUtc: dto.timestampUtc ?? dto.TimestampUtc ?? new Date().toISOString()
    };
};

const normalizeSnapshot = (snapshot) => {
    if (!snapshot) {
        return null;
    }

    return {
        orderId: snapshot.orderId ?? snapshot.OrderId,
        courierId: snapshot.courierId ?? snapshot.CourierId ?? null,
        stage: snapshot.stage ?? snapshot.Stage ?? "awaiting-courier",
        courierLocation: normalizeLocation(snapshot.courierLocation ?? snapshot.CourierLocation),
        updatedAtUtc: snapshot.updatedAtUtc ?? snapshot.UpdatedAtUtc ?? new Date().toISOString()
    };
};

export default function useCourierTracking(orderId, { enabled = true } = {}) {
    const [snapshot, setSnapshot] = useState(null);
    const [status, setStatus] = useState("idle");

    const connectionRef = useRef(null);

    useEffect(() => {
        if (!enabled || !orderId) {
            setSnapshot(null);
            setStatus("idle");
            return undefined;
        }

        let cancelled = false;

        const start = async () => {
            try {
                setStatus("connecting");

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

                connection.on("TrackingSnapshotUpdated", (nextSnapshot) => {
                    setSnapshot(normalizeSnapshot(nextSnapshot));
                    setStatus("connected");
                });

                connection.on("CourierLocationUpdated", (dto) => {
                    const location = normalizeLocation(dto);

                    setSnapshot((current) => ({
                        orderId,
                        courierId: location?.courierId ?? current?.courierId ?? null,
                        stage: current?.stage ?? "awaiting-courier",
                        courierLocation: location,
                        updatedAtUtc: location?.timestampUtc ?? new Date().toISOString()
                    }));

                    setStatus("connected");
                });

                connection.onreconnecting(() => setStatus("connecting"));
                connection.onreconnected(() => setStatus("connected"));
                connection.onclose(() => setStatus("offline"));

                await connection.start();
                if (cancelled) {
                    await connection.stop();
                    return;
                }

                await connection.invoke("SubscribeToOrder", orderId);

                connectionRef.current = connection;
                setStatus("connected");
            } catch (error) {
                console.error("Tracking subscription failed", error);
                setStatus("error");
            }
        };

        start();

        return () => {
            cancelled = true;

            const connection = connectionRef.current;
            connectionRef.current = null;

            if (connection) {
                connection.invoke("UnsubscribeFromOrder", orderId).catch(() => {});
                connection.stop().catch(() => {});
            }
        };
    }, [enabled, orderId]);

    return { snapshot, status };
}
