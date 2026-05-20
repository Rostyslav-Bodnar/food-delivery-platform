// src/pages/components/RestaurantSection.jsx
import React from 'react';
import { motion } from 'framer-motion';
import "../styles/RestaurantSection.css";

import DeliveryPaymentGrid from "./DeliveryPaymentGrid.jsx";
import DeliveryAddress from "./DeliveryAddress.jsx";

const RestaurantSection = ({
                               settings,
                               updateSettingsFor,
                               mapPosition,
                               setMapPosition,
                               mapAddress,
                               index
                           }) => {
    return (
        <motion.section initial={{ opacity: 0, y: 30 }} animate={{ opacity: 1, y: 0 }} transition={{ delay: index * 0.1 }} className="checkout-section">
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
        </motion.section>
    );
};

export default RestaurantSection;