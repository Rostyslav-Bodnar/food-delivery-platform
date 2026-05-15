// src/hooks/useLocationPicker.js
import { useState, useEffect, useCallback } from "react";

/**
 * useLocationPicker
 *
 * ✅ mapPosition  -> { lat, lng } | null     (ЄДИНЕ джерело координат)
 * ✅ mapAddress   -> string                  (ТІЛЬКИ для UI)
 * ✅ location     -> { latitude, longitude, fullAddress } | null
 *
 * ❗ ВАЖЛИВО:
 * - Логіка доставки повинна працювати ТІЛЬКИ з `location`
 * - mapAddress НІКОЛИ не використовується для обчислень
 */
const useLocationPicker = () => {
    const [mapPosition, setMapPosition] = useState(null); // { lat, lng }
    const [mapAddress, setMapAddress] = useState("");
    const [location, setLocation] = useState(null); // normalized object

    const reverseGeocode = useCallback(async (lat, lng) => {
        try {
            const res = await fetch(
                `https://nominatim.openstreetmap.org/reverse?format=json&lat=${lat}&lon=${lng}`,
                {
                    headers: {
                        "Accept": "application/json",
                        "User-Agent": "FoodDeliveryApp/1.0"
                    }
                }
            );

            if (!res.ok) {
                throw new Error("Reverse geocoding failed");
            }

            const data = await res.json();

            const address = data?.display_name ?? "";

            setMapAddress(address);

            // ✅ ЄДИНА ПРАВИЛЬНА ФОРМА ЛОКАЦІЇ ДЛЯ БЕКЕНДУ
            setLocation({
                latitude: lat,
                longitude: lng,
                fullAddress: address
            });
        } catch (err) {
            console.error("Reverse geocoding error:", err);

            // fallback — координати є, адреси нема
            setMapAddress("");
            setLocation({
                latitude: lat,
                longitude: lng,
                fullAddress: ""
            });
        }
    }, []);

    /**
     * Коли користувач клацає по мапі
     */
    useEffect(() => {
        if (!mapPosition) {
            setLocation(null);
            setMapAddress("");
            return;
        }

        reverseGeocode(mapPosition.lat, mapPosition.lng);
    }, [mapPosition, reverseGeocode]);

    /**
     * ХЕЛПЕР для скидання адреси (наприклад, при зміні типу доставки)
     */
    const clearLocation = () => {
        setMapPosition(null);
        setMapAddress("");
        setLocation(null);
    };

    return {
        // UI
        mapAddress,

        // Map
        mapPosition,
        setMapPosition,

        // ✅ ДЛЯ useOrderSubmit / backend
        location,

        // utils
        clearLocation
    };
};

export default useLocationPicker;