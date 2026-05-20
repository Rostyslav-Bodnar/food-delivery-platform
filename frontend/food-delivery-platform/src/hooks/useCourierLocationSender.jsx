import { useEffect, useRef, useState, useCallback } from "react";
import * as signalR from "@microsoft/signalr";

import { getTrackingAccessToken } from "../api/Order";
import { TRACKING_HUB_URL } from "../config/api";

const SEND_INTERVAL_MS = 5000;

const COURIER_WRITABLE_STAGES = new Set([
    "to-restaurant",
    "delivered"
]);

export default function useCourierLocationSender({
                                                     orderId,
                                                     courierId,
                                                     position,
                                                     stage,
                                                     enabled = true
                                                 }) {
    console.log("========== HOOK RENDER ==========");
    console.log({
        orderId,
        courierId,
        position,
        stage,
        enabled
    });

    const [connectionStatus, setConnectionStatus] =
        useState("idle");

    const connectionRef = useRef(null);
    const intervalRef = useRef(null);

    const latestRef = useRef({});
    const lastStageRef = useRef(null);

    const isStoppingRef = useRef(false);

    useEffect(() => {
        console.log("Updating latestRef");

        latestRef.current = {
            orderId,
            courierId,
            position,
            stage
        };

        console.log(
            "latestRef:",
            latestRef.current
        );

    }, [orderId, courierId, position, stage]);

    const invokeSafe = useCallback(
        async (method, ...args) => {

            const connection =
                connectionRef.current;

            console.log(
                `[invokeSafe] ${method}`
            );

            console.log({
                state: connection?.state,
                isStopping:
                isStoppingRef.current,
                args
            });

            if (
                isStoppingRef.current ||
                !connection ||
                connection.state !==
                signalR.HubConnectionState
                    .Connected
            ) {
                console.warn(
                    `[invokeSafe] skipped ${method}`
                );

                return false;
            }

            try {
                console.log(
                    `[invokeSafe] invoking ${method}`
                );

                await connection.invoke(
                    method,
                    ...args
                );

                console.log(
                    `[invokeSafe] success ${method}`
                );

                return true;
            } catch (error) {

                console.error(
                    `[invokeSafe] failed ${method}`,
                    error
                );

                return false;
            }

        },
        []
    );

    const publishStage = useCallback(
        async nextStage => {

            console.log(
                "[publishStage]",
                nextStage
            );

            const current =
                latestRef.current;

            console.log(
                "Current state:",
                current
            );

            if (
                !nextStage ||
                !current.orderId
            ) {
                console.warn(
                    "publishStage skipped"
                );

                return;
            }

            if (
                !COURIER_WRITABLE_STAGES.has(
                    nextStage
                )
            ) {
                console.warn(
                    "Stage not writable:",
                    nextStage
                );

                return;
            }

            if (
                lastStageRef.current ===
                nextStage
            ) {
                console.warn(
                    "Stage duplicate"
                );

                return;
            }

            const success =
                await invokeSafe(
                    "UpdateTrackingStage",
                    current.orderId,
                    nextStage
                );

            if (success) {

                lastStageRef.current =
                    nextStage;

                console.log(
                    "Stage saved:",
                    nextStage
                );
            }

        },
        [invokeSafe]
    );

    const sendLocation =
        useCallback(async () => {

            console.log(
                "[sendLocation]"
            );

            const current =
                latestRef.current;

            console.log(
                current
            );

            if (
                !current.orderId ||
                !current.courierId ||
                !current.position
            ) {

                console.warn(
                    "sendLocation skipped"
                );

                return;
            }

            await invokeSafe(
                "SendLocation",
                {
                    orderId:
                    current.orderId,
                    courierId:
                    current.courierId,
                    latitude:
                    current.position
                        .latitude,
                    longitude:
                    current.position
                        .longitude,
                    speed: null,
                    heading: null,
                    timestampUtc:
                        new Date().toISOString()
                }
            );

        }, [invokeSafe]);

    useEffect(() => {

        console.log(
            "Main effect mounted"
        );

        if (
            !enabled ||
            !orderId ||
            !courierId
        ) {

            console.warn(
                "Hook disabled"
            );

            setConnectionStatus(
                "idle"
            );

            return;
        }

        let disposed = false;

        async function cleanup() {

            console.log(
                "Cleanup started"
            );

            isStoppingRef.current =
                true;

            if (
                intervalRef.current
            ) {

                console.log(
                    "Clearing interval"
                );

                clearInterval(
                    intervalRef.current
                );

                intervalRef.current =
                    null;
            }

            const connection =
                connectionRef.current;

            connectionRef.current =
                null;

            if (!connection) {

                console.log(
                    "No connection"
                );

                return;
            }

            console.log(
                "Stopping connection:",
                connection.state
            );

            try {

                await connection.stop();

                console.log(
                    "Connection stopped"
                );

            } catch(error){

                console.error(
                    "Stop error",
                    error
                );
            }

            isStoppingRef.current =
                false;
        }

        async function startConnection() {

            try {

                console.log(
                    "Starting connection"
                );

                setConnectionStatus(
                    "connecting"
                );

                const {token} =
                    await getTrackingAccessToken(
                        orderId
                    );

                console.log(
                    "Token received"
                );

                if(disposed){

                    console.warn(
                        "Disposed before start"
                    );

                    return;
                }

                const connection =
                    new signalR.HubConnectionBuilder()
                        .withUrl(
                            TRACKING_HUB_URL,
                            {
                                accessTokenFactory:
                                    ()=>token
                            }
                        )
                        .withAutomaticReconnect([
                            0,
                            2000,
                            5000,
                            10000
                        ])
                        .configureLogging(
                            signalR.LogLevel.Information
                        )
                        .build();

                connectionRef.current =
                    connection;

                connection.onreconnecting(
                    ()=>{

                        console.warn(
                            "RECONNECTING"
                        );

                        setConnectionStatus(
                            "connecting"
                        );

                    }
                );

                connection.onreconnected(
                    ()=>{

                        console.log(
                            "RECONNECTED"
                        );

                        setConnectionStatus(
                            "connected"
                        );

                    }
                );

                connection.onclose(
                    error=>{

                        console.warn(
                            "CONNECTION CLOSED",
                            error
                        );

                        setConnectionStatus(
                            "offline"
                        );

                    }
                );

                console.log(
                    "Calling start()"
                );

                await connection.start();

                console.log(
                    "CONNECTED"
                );

                setConnectionStatus(
                    "connected"
                );

                await publishStage(
                    latestRef.current.stage
                );

                await sendLocation();

                intervalRef.current =
                    setInterval(
                        sendLocation,
                        SEND_INTERVAL_MS
                    );

                console.log(
                    "Interval started"
                );

            }
            catch(error){

                console.error(
                    "START FAILED",
                    error
                );

                setConnectionStatus(
                    "error"
                );
            }
        }

        startConnection();

        return ()=>{

            console.log(
                "Effect cleanup triggered"
            );

            disposed=true;

            cleanup();

        };

    },[
        enabled,
        orderId,
        courierId,
        publishStage,
        sendLocation
    ]);

    useEffect(()=>{

        console.log(
            "Stage changed:",
            stage
        );

        if(stage){
            publishStage(stage);
        }

    },[
        stage,
        publishStage
    ]);

    console.log(
        "connectionStatus:",
        connectionStatus
    );

    return{
        connectionStatus,
        publishStage
    };
}