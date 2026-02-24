import React from 'react';
import { Link } from 'react-router-dom';
import { motion } from 'framer-motion';
import { ShoppingCart, ArrowLeft } from 'lucide-react';

import "../styles/CartPage.css";

const EmptyCart = () => {
    return (
        <div className="cart-empty-wrapper">
            <motion.div
                initial={{ opacity: 0, scale: 0.8 }}
                animate={{ opacity: 1, scale: 1 }}
                className="cart-empty"
            >
                <ShoppingCart size={80} strokeWidth={1.2} className="empty-icon" />
                <h2>Your cart is empty</h2>
                <p>Add dishes from the menu to place an order</p>
                <Link to="/" className="back-to-menu-btn">
                    <ArrowLeft size={20} /> Back to Menu
                </Link>
            </motion.div>
        </div>
    );
};

export default EmptyCart;