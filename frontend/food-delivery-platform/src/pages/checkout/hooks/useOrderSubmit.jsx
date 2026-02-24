// src/hooks/useOrderSubmit.js
import { useNavigate } from 'react-router-dom';
import { createOrders } from "../../../api/Order.jsx";
import { clearCart } from '../../../utils/CartStorage.jsx';

const useOrderSubmit = (formData, groupedItems, getSettingsFor, mapAddress, getRestaurantTotal) => {
    const navigate = useNavigate();

    const handleSubmit = async (e) => {
        e.preventDefault();
        if (!formData.name || !formData.phone) {
            alert('Будь ласка, заповніть імʼя та телефон');
            return;
        }
        debugger;
        const now = new Date().toISOString();
        try {
            const ordersPayload = Object.entries(groupedItems).map(([restaurant, items]) => {
                const settings = getSettingsFor(restaurant);
                const finalAddress = settings.address || mapAddress;
                if (settings.deliveryType === 'delivery' && !finalAddress) {
                    throw new Error(`Адреса не вказана для ${restaurant}`);
                }
                return {
                    businessId: items[0].businessId,
                    // ❗ GUID, не string "null"
                    orderedBy: localStorage.getItem("currentAccountId"),
                    // ISO string → DateTime OK
                    orderDate: now,
                    totalPrice: getRestaurantTotal(restaurant),
                    // nullable Guid
                    deliveredBy: null,
                    // CreateLocationRequest
                    deliverFrom: {
                        fullAddress: items[0].businessAddress ?? "2, вулиця Святослава Гординського, Кант, Івано-Франківськ, Івано-Франківська міська громада, Івано-Франківський район, Івано-Франківська область, 76010, Україна"
                    },
                    // CreateLocationRequest
                    deliverTo: {
                        fullAddress: finalAddress
                    },
                    // List<CreateOrderDishRequest>
                    dishes: items.map(i => ({
                        orderId: "00000000-0000-0000-0000-000000000000",
                        dishId: i.id
                    }))
                };
            });
            await createOrders(ordersPayload);
            clearCart();
            alert("Замовлення успішно створені 🎉");
            navigate("/orders");
        } catch (err) {
            console.error(err);
            alert("Помилка при оформленні замовлення");
        }
    };

    return { handleSubmit };
};

export default useOrderSubmit;