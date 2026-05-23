import React from 'react';
import { motion, AnimatePresence } from 'framer-motion';
import { ShoppingCart } from 'lucide-react';

import "./styles/CartPage.css";
import RoleSidebar from "../../pages/sidebars/RoleSidebar.jsx";
import { useCart } from './hooks/useCart';

import EmptyCart from './components/EmptyCart';
import CartSummary from './components/CartSummary';
import CartItem from './components/CartItem';

const CartPage = () => {
    const { cartItems, totalPrice, updateQuantity, removeItem } = useCart();

    const isEmpty = cartItems.length === 0;

    return (
        <div className="app-wrapper">
            <RoleSidebar />
            <div className="cart-page-wrapper">
                <div className="cart-container">
                    <motion.h1
                        initial={{ opacity: 0, y: -30 }}
                        animate={{ opacity: 1, y: 0 }}
                        className="cart-title"
                    >
                        <ShoppingCart size={32} /> Your cart
                    </motion.h1>

                    {isEmpty ? (
                        <EmptyCart />
                    ) : (
                        <div className="cart-content">
                            <div className="cart-items">
                                <AnimatePresence>
                                    {cartItems.map((item) => (
                                        <CartItem
                                            key={item.id}
                                            item={item}
                                            updateQuantity={updateQuantity}
                                            removeItem={removeItem}
                                        />
                                    ))}
                                </AnimatePresence>
                            </div>

                            <CartSummary totalPrice={totalPrice} />
                        </div>
                    )}
                </div>
            </div>
        </div>
    );
};

export default CartPage;
