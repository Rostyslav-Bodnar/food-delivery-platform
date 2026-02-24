import { useState, useMemo } from "react";
import { getCart, saveCart } from "../../../utils/CartStorage.jsx";

export const useCart = () => {
    const [cartItems, setCartItems] = useState(getCart());

    const updateQuantity = (id, newQuantity) => {
        if (newQuantity < 1) return;
        const updated = cartItems.map(item =>
            item.id === id ? { ...item, quantity: newQuantity } : item
        );
        setCartItems(updated);
        saveCart(updated);
    };

    const removeItem = (id) => {
        const updated = cartItems.filter(item => item.id !== id);
        setCartItems(updated);
        saveCart(updated);
    };

    const totalPrice = useMemo(() =>
            cartItems.reduce((sum, item) => sum + item.price * item.quantity, 0),
        [cartItems]);

    return {
        cartItems,
        updateQuantity,
        removeItem,
        totalPrice
    };
};