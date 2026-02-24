// src/hooks/useOrderCalculations.js
const useOrderCalculations = (groupedItems, getSettingsFor) => {
    const getRestaurantSubtotal = (restaurant) =>
        groupedItems[restaurant]?.reduce((sum, i) => sum + i.price * i.quantity, 0) || 0;

    const getDeliveryCost = (paymentType) => paymentType === 'card' ? 50 : 0;

    const getRestaurantTotal = (restaurant) => {
        const settings = getSettingsFor(restaurant);
        return getRestaurantSubtotal(restaurant) + getDeliveryCost(settings.paymentType);
    };

    const getGrandTotal = () =>
        Object.keys(groupedItems).reduce((sum, r) => sum + getRestaurantTotal(r), 0);

    return { getRestaurantSubtotal, getDeliveryCost, getRestaurantTotal, getGrandTotal };
};

export default useOrderCalculations;