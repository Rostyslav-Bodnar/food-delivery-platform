// src/hooks/useOrderSubmit.js
import { useNavigate } from 'react-router-dom';
import { createOrders, getCustomerOrders } from "../../../api/Order.jsx";
import { getBusinessLocationsByBusinessId } from "../../../api/Tracking.jsx";
import { clearCart } from '../../../utils/CartStorage.jsx';
import { useState } from 'react';

const PAYMENT_API_BASE = "http://localhost:5003/api";

const toNumberOrNull = (value) => {
    const parsed = Number(value);
    return Number.isFinite(parsed) ? parsed : null;
};

const normalizeLocation = (location) => {
    if (!location) {
        return null;
    }

    const source = location.location ?? location;
    const fullAddress = source.fullAddress ?? source.address ?? null;

    if (!fullAddress) {
        return null;
    }

    return {
        fullAddress,
        address: fullAddress,
        city: source.city ?? '',
        street: source.street ?? '',
        house: source.house ?? '',
        latitude: toNumberOrNull(source.latitude ?? source.lat),
        longitude: toNumberOrNull(source.longitude ?? source.lng)
    };
};

const useOrderSubmit = (formData, groupedItems, getSettingsFor, mapAddress, getRestaurantTotal) => {
    const navigate = useNavigate();
    const [paymentState, setPaymentState] = useState({}); // { [restaurant]: { orderId, clientSecret, status: 'awaiting'|'paid' } }

    const fetchClientSecret = async (orderId) => {
        const res = await fetch(`${PAYMENT_API_BASE}/payments/${orderId}`, {
            method: 'GET',
            credentials: 'include',
            headers: { 'Accept': 'application/json' }
        });
        if (!res.ok) throw new Error("Client secret not ready");
        const json = await res.json(); // { clientSecret: "..." }
        return json.clientSecret;
    };

    const markPaid = (restaurant) => {
        setPaymentState(prev => {
            const next = { ...prev, [restaurant]: { ...prev[restaurant], status: 'paid' } };
            // якщо всі онлайн-ресторани вже "paid" → чистимо кошик і переходимо на /orders
            const onlineRestaurants = Object.keys(next).filter(r => !!next[r]?.clientSecret);
            const allPaid = onlineRestaurants.length > 0 && onlineRestaurants.every(r => next[r].status === 'paid');
            if (allPaid) {
                clearCart();
                setTimeout(() => navigate("/orders"), 400);
            }
            return next;
        });
    };

    const handleSubmit = async (e) => {
        e.preventDefault();

        // мінімальна валідація форми
        if (!formData.name || !formData.phone) {
            alert('Будь ласка, заповніть імʼя та телефон');
            return;
        }

        // обовʼязково валідний GUID
        const orderedByRaw = localStorage.getItem("currentAccountId");
        if (!orderedByRaw) {
            alert("Не знайдено ідентифікатор користувача (orderedBy). Увійдіть ще раз.");
            return;
        }
        // якщо значення збереглося із лапками — видалимо їх
        const orderedBy = orderedByRaw.replace(/^"+|"+$/g, "");

        const now = new Date().toISOString();

        try {
            const businessLocationCache = {};

            const resolveBusinessLocation = async (businessId, restaurant, items) => {
                if (businessLocationCache[businessId]) {
                    return businessLocationCache[businessId];
                }

                const locationFromCart = items
                    .map(item => normalizeLocation(item.businessLocation ?? item))
                    .find(Boolean);

                if (locationFromCart) {
                    businessLocationCache[businessId] = locationFromCart;
                    return locationFromCart;
                }

                const locations = await getBusinessLocationsByBusinessId(businessId);
                const resolvedLocation = (locations ?? [])
                    .map(normalizeLocation)
                    .find(Boolean);

                if (!resolvedLocation) {
                    throw new Error(`Не знайдено адресу закладу для ${restaurant}`);
                }

                businessLocationCache[businessId] = resolvedLocation;
                return resolvedLocation;
            };

            // 1) Формуємо МАСИВ замовлень (саме масив потрібен контролеру)
            const ordersPayload = await Promise.all(Object.entries(groupedItems).map(async ([restaurant, items]) => {
                const settings = getSettingsFor(restaurant);
                const finalAddress = settings.address || mapAddress;
                const businessLocation = await resolveBusinessLocation(items[0].businessId, restaurant, items);

                if (settings.deliveryType === 'delivery' && !finalAddress) {
                    throw new Error(`Адреса не вказана для ${restaurant}`);
                }

                // int enum: 0=Online, 1=CashOnDelivery
                const paymentMethod = settings.paymentType === 'card' ? 0 : 1;

                // допоміжний конструктор локації — кладемо і fullAddress, і address
                const toLocation = (addr) => ({
                    fullAddress: addr,
                    address: addr
                });

                return {
                    businessId: items[0].businessId,
                    orderedBy,                 // GUID
                    orderDate: now,            // ISO-string
                    totalPrice: getRestaurantTotal(restaurant),
                    deliveredBy: null,         // nullable Guid
                    deliverFrom: businessLocation,
                    deliverTo: toLocation(finalAddress),
                    paymentMethod,             // 0|1 — під DF.OrderService.Domain.Entities.PaymentMethod
                    dishes: items.map(i => ({
                        orderId: "00000000-0000-0000-0000-000000000000",
                        dishId: i.id
                    }))
                };
            }));

            // ДЕБАГ: переконайся, що це масив
            // console.log("ORDERS PAYLOAD ->", ordersPayload);

            // 2) Відправляємо масив у /order/create-orders
            //    createOrders робить POST body = ordersPayload, Content-Type = application/json
            const createdOk = await createOrders(ordersPayload);
            if (!createdOk) throw new Error("Створення замовлень повернуло помилку");

            // 3) Дістаємо всі замовлення клієнта й підбираємо щойно створені (за BusinessId+TotalPrice, найсвіжіші)
            const allCustomerOrders = await getCustomerOrders(orderedBy);

            const freshOrdersByRestaurant = {};
            for (const [restaurant, items] of Object.entries(groupedItems)) {
                const targetBusinessId = items[0].businessId;
                const expectedTotal = getRestaurantTotal(restaurant);

                const candidates = (allCustomerOrders || [])
                    .filter(o => o.businessId === targetBusinessId && Number(o.totalPrice) === Number(expectedTotal))
                    .sort((a, b) => new Date(b.orderDate) - new Date(a.orderDate));
                if (candidates.length > 0) {
                    freshOrdersByRestaurant[restaurant] = candidates[0];
                }
            }

            // 4) Для онлайн‑замовлень отримуємо client_secret з PaymentService
            const nextPaymentState = {};
            for (const [restaurant, orderObj] of Object.entries(freshOrdersByRestaurant)) {
                const settings = getSettingsFor(restaurant);
                const isOnline = settings?.paymentType === 'card';
                if (!isOnline) continue;

                let clientSecret = null;
                for (let i = 0; i < 5 && !clientSecret; i++) {
                    try {
                        clientSecret = await fetchClientSecret(orderObj.id);
                    } catch {
                        await new Promise(r => setTimeout(r, 1200)); // невеликий полінг, поки PaymentService створює PI
                    }
                }

                nextPaymentState[restaurant] = {
                    orderId: orderObj.id,
                    clientSecret: clientSecret || null,
                    status: 'awaiting'
                };
            }

            // 5) Якщо онлайн‑замовлень немає — як і раніше: очищаємо кошик та переходимо
            const onlineRestaurants = Object.keys(nextPaymentState);
            if (onlineRestaurants.length === 0) {
                clearCart();
                alert("Замовлення успішно створені 🎉");
                navigate("/orders");
                return;
            }

            // 6) Інакше показуємо Stripe Payment Element(и)
            setPaymentState(nextPaymentState);
        } catch (err) {
            console.error(err);
            alert("Помилка при оформленні замовлення");
        }
    };

    return { handleSubmit, paymentState, markPaid };
};

export default useOrderSubmit;
