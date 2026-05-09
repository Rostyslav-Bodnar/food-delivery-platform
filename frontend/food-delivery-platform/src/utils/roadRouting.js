const OSRM_BASE_URL = "https://router.project-osrm.org";

const toCoordinatePair = (point) => `${point.longitude},${point.latitude}`;

export const getRoadRoute = async (start, end) => {
    if (!start || !end) {
        return null;
    }

    const response = await fetch(
        `${OSRM_BASE_URL}/route/v1/driving/${toCoordinatePair(start)};${toCoordinatePair(end)}?overview=full&geometries=geojson`,
        {
            headers: {
                Accept: "application/json"
            }
        }
    );

    if (!response.ok) {
        throw new Error(`Routing request failed with status ${response.status}`);
    }

    const data = await response.json();
    const route = data?.routes?.[0];

    if (!route?.geometry?.coordinates?.length) {
        return null;
    }

    return {
        coordinates: route.geometry.coordinates.map(([longitude, latitude]) => [latitude, longitude]),
        distanceMeters: route.distance ?? 0,
        durationSeconds: route.duration ?? 0
    };
};
