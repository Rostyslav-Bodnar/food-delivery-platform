// src/pages/components/RestaurantSection.jsx
import React from 'react';
import { motion, AnimatePresence } from 'framer-motion';
import { ShoppingCart } from 'lucide-react';
import "../styles/RestaurantSection.css";

import OrderItem from "./OrderItem.jsx";
import DeliveryPaymentGrid from "./DeliveryPaymentGrid.jsx";
import DeliveryAddress from "./DeliveryAddress.jsx";
import SummaryBlock from "./SummaryBlock.jsx";

const RestaurantSection = ({
                               restaurant,
                               items,
                               settings,
                               updateSettingsFor,
                               handleCardChange,
                               removeItem,
                               mapPosition,
                               setMapPosition,
                               mapAddress,
                               getRestaurantSubtotal,
                               getDeliveryCost,
                               getRestaurantTotal,
                               index
                           }) => {
    return (
        <motion.section initial={{ opacity: 0, y: 30 }} animate={{ opacity: 1, y: 0 }} transition={{ delay: index * 0.1 }} className="checkout-section">
            <h2><ShoppingCart size={28} /> {restaurant}</h2>

            <div className="items-list">
                <AnimatePresence>
                    {items.map((item) => (
                        <OrderItem key={item.id} item={item} removeItem={removeItem} />
                    ))}
                </AnimatePresence>
            </div>

            <DeliveryPaymentGrid
                settings={settings}
                updateSettingsFor={updateSettingsFor}
            />

            {settings.deliveryType === 'delivery' && (
                <DeliveryAddress
                    settings={settings}
                    updateSettingsFor={updateSettingsFor}
                    mapPosition={mapPosition}
                    setMapPosition={setMapPosition}
                    mapAddress={mapAddress}
                />
            )}

            <SummaryBlock
                getRestaurantSubtotal={getRestaurantSubtotal}
                getDeliveryCost={() => getDeliveryCost(settings.paymentType)}
                getRestaurantTotal={getRestaurantTotal}
            />
        </motion.section>
    );
};

export default RestaurantSection;