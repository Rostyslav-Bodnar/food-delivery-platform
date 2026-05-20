import { useEffect, useMemo, useState } from "react";
import { calculateEtaSeconds } from "../utils/etaUtils.js";

export default function useCourierEta({
    route,
    position,
    intervalSeconds = 30
}) {
    const [tick, setTick] = useState(0);

    useEffect(() => {
        if (!route || !position) {
            return undefined;
        }

        const intervalId = window.setInterval(() => {
            setTick((value) => value + 1);
        }, intervalSeconds * 1000);

        return () => window.clearInterval(intervalId);
    }, [intervalSeconds, position, route]);

    return useMemo(
        () => calculateEtaSeconds(route, position),
        [position, route, tick]
    );
}
