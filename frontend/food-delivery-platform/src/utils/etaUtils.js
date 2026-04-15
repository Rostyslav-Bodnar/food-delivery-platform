/**
 * Calculate ETA based on remaining route distance
 * @param {Object} route - OSRM route { coordinates, distanceMeters, durationSeconds }
 * @param {Object} current - { latitude, longitude }
 * @returns {number|null} ETA seconds
 */
export function calculateEtaSeconds(route, current) {
    if (!route || !current || !route.coordinates?.length) return null;

    let remainingMeters = 0;
    let foundClosest = false;

    for (let i = 0; i < route.coordinates.length - 1; i++) {
        const [lat1, lng1] = route.coordinates[i];
        const [lat2, lng2] = route.coordinates[i + 1];

        const d1 = distanceMeters(
            current.latitude,
            current.longitude,
            lat1,
            lng1
        );

        if (!foundClosest && d1 < 30) {
            foundClosest = true;
        }

        if (foundClosest) {
            remainingMeters += distanceMeters(lat1, lng1, lat2, lng2);
        }
    }

    if (!foundClosest || remainingMeters <= 0) return route.durationSeconds;

    const avgSpeedMps =
        route.distanceMeters > 0
            ? route.distanceMeters / route.durationSeconds
            : 0;

    if (!avgSpeedMps) return null;

    return Math.round(remainingMeters / avgSpeedMps);
}

function distanceMeters(lat1, lon1, lat2, lon2) {
    const R = 6371000;
    const toRad = v => (v * Math.PI) / 180;

    const dLat = toRad(lat2 - lat1);
    const dLon = toRad(lon2 - lon1);

    const a =
        Math.sin(dLat / 2) ** 2 +
        Math.cos(toRad(lat1)) *
        Math.cos(toRad(lat2)) *
        Math.sin(dLon / 2) ** 2;

    return 2 * R * Math.atan2(Math.sqrt(a), Math.sqrt(1 - a));
}