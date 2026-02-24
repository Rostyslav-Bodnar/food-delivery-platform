// src/hooks/useRestaurantSettings.js
import { useState } from 'react';

const useRestaurantSettings = () => {
    const [restaurantSettings, setRestaurantSettings] = useState({});

    const getSettingsFor = (restaurant) => {
        if (!restaurantSettings[restaurant]) {
            return {
                deliveryType: 'delivery',
                paymentType: 'cash',
                address: '',
                cardData: { cardNumber: '', cardExpiry: '', cardCVV: '', cardName: '' }
            };
        }
        console.log(restaurantSettings[restaurant]);
        return restaurantSettings[restaurant];
    };

    const updateSettingsFor = (restaurant, updates) => {
        setRestaurantSettings(prev => ({
            ...prev,
            [restaurant]: { ...getSettingsFor(restaurant), ...updates }
        }));
    };

    const handleCardChange = (restaurant, e) => {
        const settings = getSettingsFor(restaurant);
        updateSettingsFor(restaurant, {
            cardData: { ...settings.cardData, [e.target.name]: e.target.value }
        });
    };

    return { getSettingsFor, updateSettingsFor, handleCardChange };
};

export default useRestaurantSettings;