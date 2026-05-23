const useOrderCalculations = (groupedItems, feesByRestaurant) => {
    const getRestaurantSubtotal = (restaurant) =>
        groupedItems[restaurant]?.reduce((sum, i) => sum + i.price * i.quantity, 0) || 0;

    // null while the customer hasn't picked a delivery pin yet or the lookup is
    // still in flight — UI renders a placeholder in that case.
    const getDeliveryCost = (restaurant) => {
        const fee = feesByRestaurant?.[restaurant];
        return fee == null ? null : fee;
    };

    const getRestaurantTotal = (restaurant) =>
        getRestaurantSubtotal(restaurant) + (getDeliveryCost(restaurant) ?? 0);

    const getGrandTotal = () =>
        Object.keys(groupedItems).reduce((sum, r) => sum + getRestaurantTotal(r), 0);

    return { getRestaurantSubtotal, getDeliveryCost, getRestaurantTotal, getGrandTotal };
};

export default useOrderCalculations;
