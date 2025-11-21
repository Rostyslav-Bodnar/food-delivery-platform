import React, { useState } from 'react';
import { Link } from 'react-router-dom';
import { motion, AnimatePresence } from 'framer-motion';
import { ShoppingCart, Trash2, Plus, Minus, ArrowLeft } from 'lucide-react';

// ІМПОРТ ТОЧНО ВІДПОВІДАЄ НАЗВІ ФАЙЛУ У ТЕБЕ (CartPage.css з великими літерами)
import "./styles/cartPage.css";

const CartPage = () => {
    const [cartItems, setCartItems] = useState([
        { id: 1, name: "Маргарита Піца", restaurant: "Pizza Palace", price: 249, quantity: 2, image: "https://images.unsplash.com/photo-1565299624946-b28f40a0ae38?w=500" },
        { id: 2, name: "Бургер з яловичиною", restaurant: "Burger Hub", price: 189, quantity: 1, image: "https://images.unsplash.com/photo-1568901346375-23c9450c58cd?w=500" },
        { id: 3, name: "Суші Сет Дракон", restaurant: "Sushi Master", price: 429, quantity: 1, image: "https://images.unsplash.com/photo-1579584425555-c3ce17fd4351?w=500" },
    ]);

    const updateQuantity = (id, newQuantity) => {
        if (newQuantity < 1) return;
        setCartItems(prev => prev.map(item =>
            item.id === id ? { ...item, quantity: newQuantity } : item
        ));
    };

    const removeItem = (id) => {
        setCartItems(prev => prev.filter(item => item.id !== id));
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
        <div className="cart-page-wrapper">
            <div className="particles">
                {[...Array(6)].map((_, i) => (
                    <motion.div
                        key={i}
                        className="particle"
                        initial={{ y: -100, x: Math.random() * window.innerWidth }}
                        animate={{ y: window.innerHeight + 100 }}
                        transition={{ duration: 15 + Math.random() * 10, repeat: Infinity, ease: "linear", delay: Math.random() * 5 }}
                    />
                ))}
            </div>

            <div className="cart-container">
                <motion.h1 initial={{ opacity: 0, y: -30 }} animate={{ opacity: 1, y: 0 }} className="cart-title">
                    <ShoppingCart size={32} /> Ваш кошик
                </motion.h1>

                <div className="cart-content">
                    <div className="cart-items">
                        <AnimatePresence>
                            {cartItems.map((item, index) => (
                                <motion.div
                                    key={item.id}
                                    layout
                                    initial={{ opacity: 0, x: -50 }}
                                    animate={{ opacity: 1, x: 0 }}
                                    exit={{ opacity: 0, x: 50 }}
                                    transition={{ delay: index * 0.1 }}
                                    className="cart-item-card"
                                >
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

                    <motion.div initial={{ opacity: 0, y: 30 }} animate={{ opacity: 1, y: 0 }} transition={{ delay: 0.4 }} className="cart-summary">
                        <div className="summary-row"><span>Разом:</span><strong>{totalPrice} ₴</strong></div>
                        <div className="summary-row"><span>Доставка:</span><span>Безкоштовно</span></div>
                        <div className="summary-divider" />
                        <div className="summary-row total"><span>До сплати:</span><strong className="final-price">{totalPrice} ₴</strong></div>
                        <button className="checkout-btn">Оформити замовлення</button>
                        <Link to="/" className="continue-shopping">Продовжити покупки</Link>
                    </motion.div>
                </div>
            </div>
        </div>
    );
};

export default CartPage;