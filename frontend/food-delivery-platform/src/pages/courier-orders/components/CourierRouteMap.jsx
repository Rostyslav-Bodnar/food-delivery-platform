import React from "react";
import {
    CircleMarker,
    GeoJSON,
    MapContainer,
    TileLayer,
    Tooltip,
    useMap
} from "react-leaflet";
import "leaflet/dist/leaflet.css";

const MapViewport = ({ bounds }) => {
    const map = useMap();

    React.useEffect(() => {
        if (bounds.length >= 2) {
            map.fitBounds(bounds, {
                padding: [32, 32]
            });
        } else if (bounds.length === 1) {
            map.setView(bounds[0], 14, {
                animate: true
            });
        }
    }, [bounds, map]);

    return null;
};

export default function CourierRouteMap({
    center = [50.4501, 30.5234],
    markers = [],
    route = null
}) {
    const bounds = markers.map((marker) => marker.position);
    const routeGeoJson = route?.coordinates?.length > 1
        ? {
            type: "Feature",
            geometry: {
                type: "LineString",
                coordinates: route.coordinates.map(([latitude, longitude]) => [longitude, latitude])
            },
            properties: {}
        }
        : null;

    return (
        <MapContainer
            center={center}
            zoom={13}
            scrollWheelZoom={false}
            className="courier-route-map"
        >
            <MapViewport bounds={bounds} />

            <TileLayer
                url="https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png"
                attribution="&copy; OpenStreetMap contributors"
            />

            {routeGeoJson && (
                <GeoJSON
                    data={routeGeoJson}
                    style={{
                        color: "#00d4ff",
                        weight: 5,
                        opacity: 0.82
                    }}
                />
            )}

            {markers.map((marker) => (
                <CircleMarker
                    key={marker.key}
                    center={marker.position}
                    radius={10}
                    pathOptions={{
                        color: marker.strokeColor,
                        fillColor: marker.fillColor,
                        fillOpacity: 0.9,
                        weight: 3
                    }}
                >
                    <Tooltip direction="top" offset={[0, -10]} opacity={1}>
                        {marker.label}
                    </Tooltip>
                </CircleMarker>
            ))}
        </MapContainer>
    );
}
