import { addToCart } from "../../../utils/CartStorage.jsx";

export const useAddDishToCart = () => {
    const addDish = (dish, quantity) => {
        addToCart(dish, quantity);
    };

    return { addDish };
};
