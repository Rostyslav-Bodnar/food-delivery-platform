// src/hooks/useLocationPicker.js
import { useState, useEffect } from 'react';

const useLocationPicker = () => {
    const [mapPosition, setMapPosition] = useState(null);
    const [mapAddress, setMapAddress] = useState('');

    const fetchAddressFromCoords = async ({ lat, lng }) => {
        try {
            const res = await fetch(`https://nominatim.openstreetmap.org/reverse?format=json&lat=${lat}&lon=${lng}`);
            const data = await res.json();
            if (data.display_name) setMapAddress(data.display_name);
        } catch (err) {
            console.error("Помилка отримання адреси з координатів:", err);
        }
    };

    useEffect(() => {
        if (mapPosition) fetchAddressFromCoords(mapPosition);
    }, [mapPosition]);

    return { mapPosition, setMapPosition, mapAddress };
};

export default useLocationPicker;