// src/hooks/useCart.js
import { useState, useEffect } from 'react';
import { getCart } from '../../../utils/CartStorage.jsx';

const useCart = () => {
    const [cartItems, setCartItems] = useState([]);

    useEffect(() => {
        setCartItems(getCart());
    }, []);

    const removeItem = (id) => {
        const updated = cartItems.filter(item => item.id !== id);
        setCartItems(updated);
        localStorage.setItem("cart", JSON.stringify(updated));
    };

    const groupedItems = cartItems.reduce((acc, item) => {
        if (!acc[item.restaurant]) acc[item.restaurant] = [];
        acc[item.restaurant].push(item);
        return acc;
    }, {});

    return { cartItems, removeItem, groupedItems };
};

export default useCart;