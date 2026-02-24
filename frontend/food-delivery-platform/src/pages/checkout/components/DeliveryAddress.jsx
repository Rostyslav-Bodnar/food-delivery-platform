// src/pages/components/DeliveryAddress.jsx
import React from 'react';
import { motion } from 'framer-motion';

import { MapContainer, TileLayer, Marker, useMapEvents } from 'react-leaflet';
import 'leaflet/dist/leaflet.css';
import L from 'leaflet';
import "../styles/DeliveryAddress.css";

const LocationPicker = ({ position, setPosition }) => {
    useMapEvents({
        click(e) {
            setPosition(e.latlng);
        },
    });

    return position === null ? null : (
        <Marker position={position} />
    );
};

const DeliveryAddress = ({ settings, updateSettingsFor, mapPosition, setMapPosition, mapAddress }) => {
    return (
        <div className="delivery-address-wrapper">
            <motion.input
                initial={{ opacity: 0 }}
                animate={{ opacity: 1 }}
                type="text"
                placeholder="Delivery address *"
                value={settings.address || mapAddress}
                onChange={(e) => updateSettingsFor({ address: e.target.value })}
                required
            />

            <MapContainer center={[50.45, 30.52]} zoom={12} className="leaflet-container">
                <TileLayer
                    url="https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png"
                    attribution="&copy; OpenStreetMap contributors"
                />
                <LocationPicker position={mapPosition} setPosition={setMapPosition} />
            </MapContainer>

            <small>Click on the map to select the delivery location</small>
        </div>
    );
};

export default DeliveryAddress;