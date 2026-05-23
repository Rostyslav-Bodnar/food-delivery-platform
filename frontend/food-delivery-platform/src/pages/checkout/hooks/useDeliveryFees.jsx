import { useEffect, useMemo, useState } from "react";
import { getBusinessLocationsByBusinessId } from "../../../api/BusinessLocation.ts";
import { calculateDeliveryFee } from "../../../constants/delivery.js";

const EARTH_RADIUS_KM = 6371;
const toRad = (value) => (value * Math.PI) / 180;

const haversineKm = (a, b) => {
    if (
        !a || !b ||
        !Number.isFinite(a.latitude) || !Number.isFinite(a.longitude) ||
        !Number.isFinite(b.latitude) || !Number.isFinite(b.longitude)
    ) {
        return null;
    }

    const dLat = toRad(b.latitude - a.latitude);
    const dLon = toRad(b.longitude - a.longitude);
    const lat1 = toRad(a.latitude);
    const lat2 = toRad(b.latitude);

    const h =
        Math.sin(dLat / 2) ** 2 +
        Math.cos(lat1) * Math.cos(lat2) * Math.sin(dLon / 2) ** 2;

    return 2 * EARTH_RADIUS_KM * Math.atan2(Math.sqrt(h), Math.sqrt(1 - h));
};

const normalizeBusinessLocation = (entry) => {
    const source = entry?.location ?? entry;
    if (!source) return null;
    const latitude = Number(source.latitude);
    const longitude = Number(source.longitude);
    if (!Number.isFinite(latitude) || !Number.isFinite(longitude)) return null;
    return {
        fullAddress: source.fullAddress ?? "",
        city: source.city ?? "",
        street: source.street ?? "",
        house: source.house ?? "",
        latitude,
        longitude
    };
};

const pickNearest = (locations, customer) => {
    let best = null;
    let bestDist = Infinity;
    for (const loc of locations) {
        const d = haversineKm(customer, loc);
        if (d != null && d < bestDist) {
            best = loc;
            bestDist = d;
        }
    }
    return best ? { location: best, distanceKm: bestDist } : null;
};

const useDeliveryFees = (groupedItems, customerLocation) => {
    // Stable signature so the effect doesn't re-run on every render of useCart,
    // which builds a fresh groupedItems object reference each time.
    const cartSignature = useMemo(() => {
        return Object.entries(groupedItems ?? {})
            .map(([restaurant, items]) => {
                const businessId = items?.[0]?.businessId ?? "";
                const dishes = items
                    .map((i) => `${i.id}x${i.quantity ?? 1}@${i.price}`)
                    .sort()
                    .join(",");
                return `${restaurant}|${businessId}|${dishes}`;
            })
            .sort()
            .join("||");
    }, [groupedItems]);

    const customerLat = customerLocation?.latitude;
    const customerLng = customerLocation?.longitude;

    const [feesByRestaurant, setFeesByRestaurant] = useState({});
    const [resolvedByRestaurant, setResolvedByRestaurant] = useState({});
    const [loading, setLoading] = useState(false);

    useEffect(() => {
        const restaurants = Object.keys(groupedItems ?? {});
        const hasCustomerCoords =
            Number.isFinite(customerLat) && Number.isFinite(customerLng);

        if (!hasCustomerCoords || restaurants.length === 0) {
            setFeesByRestaurant({});
            setResolvedByRestaurant({});
            return undefined;
        }

        const customer = { latitude: customerLat, longitude: customerLng };
        let cancelled = false;
        setLoading(true);

        (async () => {
            const fees = {};
            const resolved = {};

            await Promise.all(restaurants.map(async (restaurant) => {
                const items = groupedItems[restaurant];
                const businessId = items?.[0]?.businessId;
                if (!businessId) return;

                try {
                    const raw = await getBusinessLocationsByBusinessId(businessId);
                    const candidates = (raw ?? [])
                        .map(normalizeBusinessLocation)
                        .filter(Boolean);

                    const nearest = pickNearest(candidates, customer);
                    if (!nearest) return;

                    const subtotal = items.reduce(
                        (sum, i) => sum + (Number(i.price) || 0) * (Number(i.quantity) || 1),
                        0
                    );
                    const fee = calculateDeliveryFee(subtotal, nearest.distanceKm);
                    if (fee == null) return;

                    fees[restaurant] = fee;
                    resolved[restaurant] = {
                        businessLocation: nearest.location,
                        distanceKm: nearest.distanceKm
                    };
                } catch (err) {
                    console.error(`Failed to resolve delivery fee for ${restaurant}`, err);
                }
            }));

            if (!cancelled) {
                setFeesByRestaurant(fees);
                setResolvedByRestaurant(resolved);
                setLoading(false);
            }
        })();

        return () => {
            cancelled = true;
        };
    }, [cartSignature, customerLat, customerLng]);

    return { feesByRestaurant, resolvedByRestaurant, loading };
};

export default useDeliveryFees;
