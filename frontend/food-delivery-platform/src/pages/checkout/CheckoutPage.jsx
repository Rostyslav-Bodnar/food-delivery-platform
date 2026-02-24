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

const CheckoutPage = () => {
    const { groupedItems, removeItem } = useCart();
    const { formData, handleInputChange } = useCheckoutForm();
    const { getSettingsFor, updateSettingsFor, handleCardChange } = useRestaurantSettings();
    const { mapPosition, setMapPosition, mapAddress } = useLocationPicker();
    const { getRestaurantSubtotal, getDeliveryCost, getRestaurantTotal, getGrandTotal } = useOrderCalculations(groupedItems, getSettingsFor);
    const { handleSubmit } = useOrderSubmit(formData, groupedItems, getSettingsFor, mapAddress, getRestaurantTotal);

    return (
        <div className="app-wrapper">
            <CustomerSidebar />
            <div className="checkout-page-wrapper">
                <ParticlesBackground />
                <div className="checkout-container">
                    <motion.h1 initial={{ opacity: 0, y: -30 }} animate={{ opacity: 1, y: 0 }} className="checkout-title">
                        <CreditCard size={36} /> Оформлення замовлення
                    </motion.h1>
                    <form onSubmit={handleSubmit}>
                        <ContactInfo formData={formData} handleInputChange={handleInputChange} />
                        {/* Блоки по ресторанах */}
                        {Object.entries(groupedItems).map(([restaurant, items], index) => (
                            <RestaurantSection
                                key={restaurant}
                                restaurant={restaurant}
                                items={items}
                                settings={getSettingsFor(restaurant)}
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
                        ))}
                        <CommentSection formData={formData} handleInputChange={handleInputChange} />
                        <TotalSection getGrandTotal={getGrandTotal} />
                    </form>
                    <BottomLinks />
                </div>
            </div>
        </div>
    );
};
export default CheckoutPage;