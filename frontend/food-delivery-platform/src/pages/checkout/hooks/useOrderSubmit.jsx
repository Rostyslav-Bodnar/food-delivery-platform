// src/hooks/useOrderSubmit.js
import { useNavigate } from "react-router-dom";
import { useState } from "react";

import { createOrders, getCustomerOrders } from "../../../api/Order.ts";
import { getBusinessLocationsByBusinessId } from "../../../api/BusinessLocation.ts";
import { clearCart } from "../../../utils/CartStorage.jsx";

const PAYMENT_API_BASE = "http://localhost:5003/api";

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
    getRestaurantTotal
) => {
    const navigate = useNavigate();
    const [paymentState, setPaymentState] = useState({});

    /* -------- Stripe -------- */

    const fetchClientSecret = async (orderId) => {
        const res = await fetch(`${PAYMENT_API_BASE}/payments/${orderId}`, {
            method: "GET",
            credentials: "include",
            headers: { Accept: "application/json" }
        });

        if (!res.ok) throw new Error("Client secret not ready");

        const json = await res.json();
        return json.clientSecret;
    };

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
            alert("Будь ласка, заповніть імʼя та телефон");
            return;
        }

        const orderedByRaw = localStorage.getItem("currentAccountId");
        if (!orderedByRaw) {
            alert("Не знайдено ідентифікатор користувача");
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
                if (businessLocationCache[businessId]) {
                    return businessLocationCache[businessId];
                }

                if (!customerLocation?.latitude || !customerLocation?.longitude) {
                    throw new Error(
                        `Адреса клієнта не має координат (restaurant=${restaurant})`
                    );
                }

                const locations = await getBusinessLocationsByBusinessId(businessId);
                const normalized = (locations ?? [])
                    .map(normalizeLocation)
                    .filter(
                        (l) => l?.latitude != null && l?.longitude != null
                    );

                if (normalized.length === 0) {
                    throw new Error(`У закладу ${restaurant} немає валідних локацій`);
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
                        throw new Error(`Адреса доставки не вибрана для ${restaurant}`);
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
                        dishes: items.map((i) => ({
                            orderId: "00000000-0000-0000-0000-000000000000",
                            dishId: i.id
                        }))
                    };
                })
            );

            /* -------- create orders -------- */

            const createdOk = await createOrders(ordersPayload);
            if (!createdOk) throw new Error("Помилка створення замовлення");

            /* -------- fetch fresh orders -------- */

            const allOrders = await getCustomerOrders(orderedBy);
            const freshOrders = {};

            for (const [restaurant, items] of Object.entries(groupedItems)) {
                const bid = items[0].businessId;
                const total = getRestaurantTotal(restaurant);

                const found = (allOrders ?? [])
                    .filter(
                        (o) =>
                            o.businessId === bid &&
                            Number(o.totalPrice) === Number(total)
                    )
                    .sort(
                        (a, b) =>
                            new Date(b.orderDate) - new Date(a.orderDate)
                    )[0];

                if (found) {
                    freshOrders[restaurant] = found;
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
                alert("Замовлення успішно створені 🎉");
                navigate("/orders");
                return;
            }

            setPaymentState(nextPaymentState);
        } catch (err) {
            console.error(err);
            alert(err.message || "Помилка при оформленні замовлення");
        }
    };

    return { handleSubmit, paymentState, markPaid };
};

export default useOrderSubmit;