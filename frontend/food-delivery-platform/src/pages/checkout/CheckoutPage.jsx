// src/pages/CheckoutPage.jsx
import React from 'react';
import { CreditCard } from 'lucide-react';
import "./styles/CheckoutPage.css";
import { motion } from 'framer-motion';
import CustomerSidebar from "../sidebars/CustomerSidebar.jsx";
import ParticlesBackground from "./components/ParticlesBackground.jsx";
import ContactInfo from "./components/ContactInfo.jsx";
import RestaurantSection from "./components/RestaurantSection.jsx";
import CommentSection from "./components/CommentSection.jsx";
import TotalSection from "./components/TotalSection.jsx";
import BottomLinks from "./components/BottomLinks.jsx";

import useCart from "./hooks/useCart";
import useCheckoutForm from "./hooks/useCheckoutForm";
import useRestaurantSettings from "./hooks/useRestaurantSettings";
import useLocationPicker from "./hooks/useLocationPicker";
import useOrderCalculations from "./hooks/useOrderCalculations";
import useOrderSubmit from "./hooks/useOrderSubmit";

// 🔹 Stripe модалка + стилі
import StripePaymentModal from "./components/StripePaymentModal.jsx";
import "./styles/StripePaymentModal.css";

const CheckoutPage = () => {
    const { groupedItems, removeItem } = useCart();
    const { formData, handleInputChange } = useCheckoutForm();
    const { getSettingsFor, updateSettingsFor, handleCardChange } = useRestaurantSettings();
    const {
        mapPosition,
        setMapPosition,
        mapAddress,
        location
    } = useLocationPicker();
    const {
        getRestaurantSubtotal,
        getDeliveryCost,
        getRestaurantTotal,
        getGrandTotal
    } = useOrderCalculations(groupedItems, getSettingsFor);

    const { handleSubmit, paymentState, markPaid } =
        useOrderSubmit(formData, groupedItems, getSettingsFor, location, getRestaurantTotal);

    // Яка модалка відкрита (ключ — назва ресторану)
    const [openForRestaurant, setOpenForRestaurant] = React.useState(null);

    // Множина ресторанів, для яких користувач закрив модалку вручну (щоб не авто‑відкривати знову)
    const [dismissedRestaurants, setDismissedRestaurants] = React.useState(() => new Set());

    // Відкрити модалку для ресторану та зняти "dismissed", якщо він там був
    const openModalFor = (restaurant) => {
        setOpenForRestaurant(restaurant);
        setDismissedRestaurants(prev => {
            const next = new Set(prev);
            next.delete(restaurant);
            return next;
        });
    };

    // Закрити модалку вручну та запам'ятати, що для цього ресторану не треба авто‑відкривати знову
    const closeModal = () => {
        if (openForRestaurant) {
            setDismissedRestaurants(prev => {
                const next = new Set(prev);
                next.add(openForRestaurant);
                return next;
            });
        }
        setOpenForRestaurant(null);
    };

    // 🔸 Автовідкриття модалки: як тільки з'являється clientSecret для першого онлайн-ресторана без статусу 'paid'
    //     та який НЕ був вручну закритий (dismissed)
    React.useEffect(() => {
        if (openForRestaurant) return; // якщо вже відкрита — не перекидати
        if (!paymentState || Object.keys(paymentState).length === 0) return;

        const nextToPay = Object.entries(paymentState).find(([restaurant, st]) =>
            st?.clientSecret &&
            st?.status !== 'paid' &&
            !dismissedRestaurants.has(restaurant)
        );

        if (nextToPay) {
            setOpenForRestaurant(nextToPay[0]);
        }
    }, [paymentState, openForRestaurant, dismissedRestaurants]);

    // 🔸 Після успішної оплати для ресторану — позначити 'paid' і, якщо є інші — ефект вище відкриє наступний
    //     НЕ додаємо в dismissed, щоб автологіка змогла відкрити наступний.
    const handlePaidAndMaybeOpenNext = (restaurant) => {
        setOpenForRestaurant(null);
        markPaid(restaurant); // ваш хук потім перевірить, чи всі оплачені, очистить кошик і перекине на /orders
    };

    return (
        <div className="app-wrapper">
            <CustomerSidebar />
            <div className="checkout-page-wrapper">
                <ParticlesBackground />
                <div className="checkout-container">
                    <motion.h1
                        initial={{ opacity: 0, y: -30 }}
                        animate={{ opacity: 1, y: 0 }}
                        className="checkout-title"
                    >
                        <CreditCard size={36} /> Оформлення замовлення
                    </motion.h1>

                    {/* 1) Submit: створюємо ордери; 2) якщо є онлайн-оплати — зʼявляться clientSecret, і модалка відкриється сама */}
                    <form onSubmit={handleSubmit}>
                        <ContactInfo formData={formData} handleInputChange={handleInputChange} />

                        {/* Блоки по ресторанах */}
                        {Object.entries(groupedItems).map(([restaurant, items], index) => {
                            const settings = getSettingsFor(restaurant);
                            const payOnline = settings?.paymentType === 'card';
                            const clientSecret = paymentState?.[restaurant]?.clientSecret;

                            return (
                                <div key={restaurant}>
                                    <RestaurantSection
                                        restaurant={restaurant}
                                        items={items}
                                        settings={settings}
                                        updateSettingsFor={(updates) => updateSettingsFor(restaurant, updates)}
                                        handleCardChange={(e) => handleCardChange(restaurant, e)}
                                        removeItem={removeItem}
                                        mapPosition={mapPosition}
                                        setMapPosition={setMapPosition}
                                        mapAddress={mapAddress}
                                        getRestaurantSubtotal={() => getRestaurantSubtotal(restaurant)}
                                        getDeliveryCost={(paymentType) => getDeliveryCost(paymentType)}
                                        getRestaurantTotal={() => getRestaurantTotal(restaurant)}
                                        index={index}
                                    />

                                    {/* Інформація про підготовку оплати / повторне відкриття модалки (якщо юзер закрив) */}
                                    {payOnline && !clientSecret && (
                                        <div style={{ marginTop: 12, color: '#9ca3af' }}>
                                            Готуємо форму оплати для <b>{restaurant}</b>...
                                        </div>
                                    )}

                                    {payOnline && clientSecret && openForRestaurant !== restaurant && (
                                        <div style={{ marginTop: 12, color: '#9ca3af', marginBottom: 12 }}>
                                            Форма оплати для <b>{restaurant}</b> готова.&nbsp;
                                            <button
                                                type="button"
                                                style={{
                                                    background: "transparent",
                                                    border: "none",
                                                    color: "#9aa9ff",
                                                    textDecoration: "underline",
                                                    cursor: "pointer",
                                                    padding: 0
                                                }}
                                                onClick={() => openModalFor(restaurant)}
                                            >
                                                Відкрити оплату
                                            </button>
                                        </div>
                                    )}
                                </div>
                            );
                        })}
                        <TotalSection getGrandTotal={getGrandTotal} />
                    </form>

                    <BottomLinks />
                </div>
            </div>

            {/* 🔹 Одна модалка: показуємо для поточного ресторану, який потребує оплати */}
            {openForRestaurant && paymentState?.[openForRestaurant]?.clientSecret && (
                <StripePaymentModal
                    open={true}
                    onClose={closeModal}
                    clientSecret={paymentState[openForRestaurant].clientSecret}
                    title={`Оплата для «${openForRestaurant}»`}
                    subtitle="Ваші дані захищені. Можлива перевірка 3D Secure."
                    onPaid={() => handlePaidAndMaybeOpenNext(openForRestaurant)}
                />
            )}
        </div>
    );
};

export default CheckoutPage;
``