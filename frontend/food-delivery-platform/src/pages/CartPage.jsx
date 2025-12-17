import React, { useState } from 'react';
import { Link } from 'react-router-dom';
import { motion, AnimatePresence } from 'framer-motion';
import { ShoppingCart, Trash2, Plus, Minus, ArrowLeft } from 'lucide-react';

import "./styles/CartPage.css";
import CustomerSidebar from "../components/customer-components/CustomerSidebar.jsx";
import { getCart, saveCart } from "../utils/CartStorage.jsx";

const CartPage = () => {
    const [cartItems, setCartItems] = useState(getCart());
    console.log(cartItems);

    const updateQuantity = (id, newQuantity) => {
        if (newQuantity < 1) return;
        const updated = cartItems.map(item =>
            item.id === id ? { ...item, quantity: newQuantity } : item
        );
        setCartItems(updated);
        saveCart(updated);
        console.log(cartItems);
    };

    const removeItem = (id) => {
        const updated = cartItems.filter(item => item.id !== id);
        setCartItems(updated);
        saveCart(updated);
    };

    const totalPrice = cartItems.reduce((sum, item) => sum + item.price * item.quantity, 0);

    if (cartItems.length === 0) {
        return (
            <div className="cart-empty-wrapper">
                <motion.div initial={{ opacity: 0, scale: 0.8 }} animate={{ opacity: 1, scale: 1 }} className="cart-empty">
                    <ShoppingCart size={80} strokeWidth={1.2} className="empty-icon" />
                    <h2>Ваш кошик порожній</h2>
                    <p>Додайте страви з меню, щоб зробити замовлення</p>
                    <Link to="/" className="back-to-menu-btn">
                        <ArrowLeft size={20} /> Повернутись до меню
                    </Link>
                </motion.div>
            </div>
        );
    }

    return (
        <div className="app-wrapper">
            <CustomerSidebar />
            <div className="cart-page-wrapper">
                <div className="cart-container">
                    <motion.h1 initial={{ opacity: 0, y: -30 }} animate={{ opacity: 1, y: 0 }} className="cart-title">
                        <ShoppingCart size={32} /> Ваш кошик
                    </motion.h1>

                    <div className="cart-content">
                        <div className="cart-items">
                            <AnimatePresence>
                                {cartItems.map((item, index) => (
                                    <motion.div key={item.id} layout className="cart-item-card">
                                        <img src={item.image} alt={item.name} className="cart-item-image" />
                                        <div className="cart-item-info">
                                            <h3>{item.name}</h3>
                                            <p className="cart-restaurant">{item.restaurant}</p>
                                            <div className="quantity-controls">
                                                <button onClick={() => updateQuantity(item.id, item.quantity - 1)}><Minus size={16} /></button>
                                                <span className="quantity">{item.quantity}</span>
                                                <button onClick={() => updateQuantity(item.id, item.quantity + 1)}><Plus size={16} /></button>
                                            </div>
                                        </div>
                                        <div className="cart-item-price">
                                            <p>{item.price * item.quantity} ₴</p>
                                            <button onClick={() => removeItem(item.id)} className="remove-btn"><Trash2 size={18} /></button>
                                        </div>
                                    </motion.div>
                                ))}
                            </AnimatePresence>
                        </div>

                        <motion.div className="cart-summary">
                            <div className="summary-row"><span>Разом:</span><strong>{totalPrice} ₴</strong></div>
                            <div className="summary-row"><span>Доставка:</span><span>Безкоштовно</span></div>
                            <div className="summary-divider" />
                            <div className="summary-row total"><span>До сплати:</span><strong className="final-price">{totalPrice} ₴</strong></div>
                            <Link to="/checkout" className="checkout-btn">Оформити замовлення</Link>
                            <Link to="/" className="continue-shopping">Продовжити покупки</Link>
                        </motion.div>
                    </div>
                </div>
            </div>
        </div>
    );
};

export default CartPage;
