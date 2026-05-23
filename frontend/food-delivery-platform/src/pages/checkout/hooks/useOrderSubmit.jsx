// src/hooks/useOrderSubmit.js
import { useNavigate } from "react-router-dom";
import { useState } from "react";

import { createOrders, getCustomerOrders } from "../../../api/Order.ts";
import { getBusinessLocationsByBusinessId } from "../../../api/BusinessLocation.ts";
import { getClientSecret } from "../../../api/Payment.jsx";
import { clearCart } from "../../../utils/CartStorage.jsx";

/* -------------------- helpers -------------------- */

const toNumberOrNull = (value) => {
    const parsed = Number(value);
    return Number.isFinite(parsed) ? parsed : null;
};

const normalizeLocation = (location) => {
    if (!location) return null;

    const source = location.location ?? location;
    const fullAddress = source.fullAddress ?? source.address ?? source ?? null;
    if (!fullAddress) return null;

    return {
        fullAddress,
        address: fullAddress,
        city: source.city ?? "",
        street: source.street ?? "",
        house: source.house ?? "",
        latitude: toNumberOrNull(source.latitude ?? source.lat),
        longitude: toNumberOrNull(source.longitude ?? source.lng)
    };
};

const haversineKm = (a, b) => {
    if (!a || !b || !a.latitude || !a.longitude || !b.latitude || !b.longitude) {
        return Infinity;
    }

    const toRad = (x) => (x * Math.PI) / 180;
    const R = 6371;

    const dLat = toRad(b.latitude - a.latitude);
    const dLon = toRad(b.longitude - a.longitude);

    const lat1 = toRad(a.latitude);
    const lat2 = toRad(b.latitude);

    const h =
        Math.sin(dLat / 2) ** 2 +
        Math.cos(lat1) * Math.cos(lat2) * Math.sin(dLon / 2) ** 2;

    return 2 * R * Math.atan2(Math.sqrt(h), Math.sqrt(1 - h));
};

/* -------------------- hook -------------------- */

const useOrderSubmit = (
    formData,
    groupedItems,
    getSettingsFor,
    mapAddress,
    getRestaurantTotal,
    resolvedByRestaurant
) => {
    const navigate = useNavigate();
    const [paymentState, setPaymentState] = useState({});

    /* -------- Stripe -------- */

    const fetchClientSecret = (orderId) => getClientSecret(orderId);

    const markPaid = (restaurant) => {
        setPaymentState((prev) => {
            const next = {
                ...prev,
                [restaurant]: { ...prev[restaurant], status: "paid" }
            };

            const online = Object.keys(next).filter(
                (r) => !!next[r]?.clientSecret
            );

            const allPaid =
                online.length > 0 &&
                online.every((r) => next[r].status === "paid");

            if (allPaid) {
                clearCart();
                setTimeout(() => navigate("/orders"), 400);
            }

            return next;
        });
    };

    /* -------- submit -------- */

    const handleSubmit = async (e) => {
        e.preventDefault();

        if (!formData.name || !formData.phone) {
            alert("Please enter your name and phone number");
            return;
        }

        const orderedByRaw = localStorage.getItem("currentAccountId");
        if (!orderedByRaw) {
            alert("User ID not found");
            return;
        }

        const orderedBy = orderedByRaw.replace(/^"+|"+$/g, "");
        const now = new Date().toISOString();

        try {
            const businessLocationCache = {};

            const resolveBusinessLocation = async (
                businessId,
                restaurant,
                customerLocation
            ) => {
                // useDeliveryFees may have already resolved the nearest location for
                // delivery-fee preview; reuse it to avoid a second roundtrip.
                const precomputed = resolvedByRestaurant?.[restaurant]?.businessLocation;
                if (precomputed) {
                    return precomputed;
                }

                if (businessLocationCache[businessId]) {
                    return businessLocationCache[businessId];
                }

                if (!customerLocation?.latitude || !customerLocation?.longitude) {
                    throw new Error(
                        `The customer's address does not include coordinates (restaurant=${restaurant})`
                    );
                }

                const locations = await getBusinessLocationsByBusinessId(businessId);
                const normalized = (locations ?? [])
                    .map(normalizeLocation)
                    .filter(
                        (l) => l?.latitude != null && l?.longitude != null
                    );

                if (normalized.length === 0) {
                    throw new Error(`The restaurant ${restaurant} has no valid locations`);
                }

                let best = normalized[0];
                let bestDist = haversineKm(customerLocation, best);

                for (const loc of normalized.slice(1)) {
                    const d = haversineKm(customerLocation, loc);
                    if (d < bestDist) {
                        bestDist = d;
                        best = loc;
                    }
                }

                businessLocationCache[businessId] = best;
                return best;
            };

            /* -------- build orders payload -------- */

            const ordersPayload = await Promise.all(
                Object.entries(groupedItems).map(async ([restaurant, items]) => {
                    const settings = getSettingsFor(restaurant);

                    const customerLocation = normalizeLocation(mapAddress);
                    if (settings.deliveryType === "delivery" && !customerLocation) {
                        throw new Error(`No shipping address has been selected for ${restaurant}`);
                    }

                    const businessLocation = await resolveBusinessLocation(
                        items[0].businessId,
                        restaurant,
                        customerLocation
                    );

                    const paymentMethod =
                        settings.paymentType === "card" ? 0 : 1; // enum OK

                    return {
                        businessId: items[0].businessId,       // Guid ✅
                        orderedBy,                             // Guid ✅
                        orderDate: now,                        // ISO → DateTime ✅
                        totalPrice: getRestaurantTotal(restaurant), // decimal ✅
                        deliveredBy: null,
                        deliverFrom: {
                            fullAddress: businessLocation.fullAddress
                        },
                        deliverTo: {
                            fullAddress: customerLocation.fullAddress
                        },
                        paymentMethod,
                        // Group duplicate dish entries by dishId so the server sees one
                        // OrderedDish row with a real Quantity rather than N duplicate rows.
                        dishes: Object.values(items.reduce((acc, i) => {
                            if (acc[i.id]) {
                                acc[i.id].quantity += i.quantity ?? 1;
                            } else {
                                acc[i.id] = {
                                    orderId: "00000000-0000-0000-0000-000000000000",
                                    dishId: i.id,
                                    quantity: i.quantity ?? 1
                                };
                            }
                            return acc;
                        }, {}))
                    };
                })
            );

            /* -------- create orders -------- */

            const createdOk = await createOrders(ordersPayload);
            if (!createdOk) throw new Error("Order creation error");

            /* -------- fetch fresh orders -------- */

            const allOrders = await getCustomerOrders(orderedBy);
            const freshOrders = {};
            // OrderService now recomputes TotalPrice server-side (dish subtotal only;
            // delivery fee is added later by LocationsCreatedConsumer), so the cart's
            // grand total no longer matches o.totalPrice. Match by businessId + recency
            // and consume orders one at a time so multiple restaurants in the same
            // checkout each pick a distinct row.
            const consumedIds = new Set();
            for (const [restaurant, items] of Object.entries(groupedItems)) {
                const bid = items[0].businessId;

                const found = (allOrders ?? [])
                    .filter((o) => o.businessId === bid && !consumedIds.has(o.id))
                    .sort(
                        (a, b) =>
                            new Date(b.orderDate) - new Date(a.orderDate)
                    )[0];

                if (found) {
                    freshOrders[restaurant] = found;
                    consumedIds.add(found.id);
                }
            }

            /* -------- stripe payment -------- */

            const nextPaymentState = {};

            for (const [restaurant, order] of Object.entries(freshOrders)) {
                const settings = getSettingsFor(restaurant);
                if (settings.paymentType !== "card") continue;

                let clientSecret = null;

                for (let i = 0; i < 5 && !clientSecret; i++) {
                    try {
                        clientSecret = await fetchClientSecret(order.id);
                    } catch {
                        await new Promise((r) => setTimeout(r, 1200));
                    }
                }

                nextPaymentState[restaurant] = {
                    orderId: order.id,
                    clientSecret,
                    status: "awaiting"
                };
            }

            if (Object.keys(nextPaymentState).length === 0) {
                clearCart();
                //alert("Замовлення успішно створені 🎉");
                //navigate("/orders");
                return;
            }

            setPaymentState(nextPaymentState);
        } catch (err) {
            console.error(err);
            alert(err.message || "An error occurred while placing your order");
        }
    };

    return { handleSubmit, paymentState, markPaid };
};

export default useOrderSubmit;
