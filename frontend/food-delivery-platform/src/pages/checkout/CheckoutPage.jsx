// src/pages/CheckoutPage.jsx
import React from 'react';
import { CreditCard } from 'lucide-react';
import "./styles/CheckoutPage.css";
import {AnimatePresence, motion} from 'framer-motion';
import RoleSidebar from "../sidebars/RoleSidebar.jsx";
import ParticlesBackground from "./components/ParticlesBackground.jsx";
import ContactInfo from "./components/ContactInfo.jsx";
import RestaurantSection from "./components/RestaurantSection.jsx";
import TotalSection from "./components/TotalSection.jsx";
import BottomLinks from "./components/BottomLinks.jsx";

import useCart from "./hooks/useCart";
import useCheckoutForm from "./hooks/useCheckoutForm";
import useRestaurantSettings from "./hooks/useRestaurantSettings";
import useLocationPicker from "./hooks/useLocationPicker";
import useOrderCalculations from "./hooks/useOrderCalculations";
import useOrderSubmit from "./hooks/useOrderSubmit";
import useDeliveryFees from "./hooks/useDeliveryFees";

// 🔹 Stripe модалка + стилі
import StripePaymentModal from "./components/StripePaymentModal.jsx";
import "./styles/StripePaymentModal.css";
import SummaryBlock from "./components/SummaryBlock.jsx";
import OrderItem from "./components/OrderItem.jsx";

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
    const { feesByRestaurant, resolvedByRestaurant } =
        useDeliveryFees(groupedItems, location, getSettingsFor);

    const {
        getRestaurantSubtotal,
        getDeliveryCost,
        getRestaurantTotal,
        getGrandTotal
    } = useOrderCalculations(groupedItems, feesByRestaurant);

    const { handleSubmit, paymentState, markPaid, submitting } =
        useOrderSubmit(formData, groupedItems, getSettingsFor, location, getRestaurantTotal, resolvedByRestaurant);

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
            <RoleSidebar />
            <div className="checkout-page-wrapper">
                <ParticlesBackground />
                <div className="checkout-container">
                    <motion.h1
                        initial={{ opacity: 0, y: -30 }}
                        animate={{ opacity: 1, y: 0 }}
                        className="checkout-title"
                    >
                        <CreditCard size={36} /> Placing an Order
                    </motion.h1>

                    {/* 1) Submit: створюємо ордери; 2) якщо є онлайн-оплати — зʼявляться clientSecret, і модалка відкриється сама */}
                    <form onSubmit={handleSubmit} className="checkout-grid">

                        {/* ✅ LEFT */}
                        <div className="checkout-left">
                            <ContactInfo
                                formData={formData}
                                handleInputChange={handleInputChange}
                            />

                            {Object.entries(groupedItems).map(([restaurant, items], index) => {
                                const settings = getSettingsFor(restaurant);

                                return (
                                    <RestaurantSection
                                        key={restaurant}
                                        settings={settings}
                                        updateSettingsFor={(updates) => updateSettingsFor(restaurant, updates)}
                                        mapPosition={mapPosition}
                                        setMapPosition={setMapPosition}
                                        mapAddress={mapAddress}
                                        index={index}
                                    />
                                );
                            })}
                            <BottomLinks />

                        </div>

                        {/* ✅ RIGHT */}
                        <div className="checkout-right">
                            <div className="checkout-right-inner">

                                {Object.entries(groupedItems).map(([restaurant, items]) => {
                                    const settings = getSettingsFor(restaurant);

                                    return (
                                        <div key={restaurant} className="summary-card">

                                            <h3 className="summary-title">
                                                {restaurant}
                                            </h3>

                                            <AnimatePresence>
                                                {items.map((item) => (
                                                    <OrderItem
                                                        key={item.id}
                                                        item={item}
                                                        removeItem={removeItem}
                                                    />
                                                ))}
                                            </AnimatePresence>

                                            <SummaryBlock
                                                getRestaurantSubtotal={() => getRestaurantSubtotal(restaurant)}
                                                getDeliveryCost={() => getDeliveryCost(restaurant)}
                                                getRestaurantTotal={() => getRestaurantTotal(restaurant)}
                                            />

                                        </div>
                                    );
                                })}

                                <TotalSection getGrandTotal={getGrandTotal} submitting={submitting} />

                            </div>
                        </div>

                    </form>
                </div>
            </div>

            {/* 🔹 Одна модалка: показуємо для поточного ресторану, який потребує оплати */}
            {openForRestaurant && paymentState?.[openForRestaurant]?.clientSecret && (
                <StripePaymentModal
                    open={true}
                    onClose={closeModal}
                    clientSecret={paymentState[openForRestaurant].clientSecret}
                    title={`Payment for «${openForRestaurant}»`}
                    subtitle="Your information is secure. 3D Secure verification may be required."
                    onPaid={() => handlePaidAndMaybeOpenNext(openForRestaurant)}
                />
            )}
        </div>
    );
};

export default CheckoutPage;