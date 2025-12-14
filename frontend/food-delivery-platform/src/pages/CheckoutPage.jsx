// src/pages/CheckoutPage.jsx
import React, { useState } from 'react';
import { Link } from 'react-router-dom';
import { motion, AnimatePresence } from 'framer-motion';
import {
    ShoppingCart, User, Truck, Store, CreditCard,
    DollarSign, Trash2, ArrowLeft
} from 'lucide-react';

import "./styles/CheckoutPage.css";
import CustomerSidebar from "../components/customer-components/CustomerSidebar.jsx";

const CheckoutPage = () => {
    const [cartItems, setCartItems] = useState([
        { id: 1, name: "Маргарита Піца", restaurant: "Pizza Palace", price: 249, quantity: 2, image: "https://images.unsplash.com/photo-1565299624946-b28f40a0ae38?w=500" },
        { id: 2, name: "Бургер з яловичиною", restaurant: "Burger Hub", price: 189, quantity: 1, image: "https://images.unsplash.com/photo-1568901346375-23c9450c58cd?w=500" },
        { id: 3, name: "Суші Сет Дракон", restaurant: "Sushi Master", price: 429, quantity: 1, image: "https://images.unsplash.com/photo-1579584425555-c3ce17fd4351?w=500" },
    ]);

    const [formData, setFormData] = useState({
        name: '', phone: '', email: '', comment: ''
    });

    const [restaurantSettings, setRestaurantSettings] = useState({});

    const getSettingsFor = (restaurant) => {
        if (!restaurantSettings[restaurant]) {
            return { deliveryType: 'delivery', paymentType: 'cash', address: '', cardData: { cardNumber: '', cardExpiry: '', cardCVV: '', cardName: '' } };
        }
        return restaurantSettings[restaurant];
    };

    const updateSettingsFor = (restaurant, updates) => {
        setRestaurantSettings(prev => ({
            ...prev,
            [restaurant]: { ...getSettingsFor(restaurant), ...updates }
        }));
    };

    const handleInputChange = (e) => {
        setFormData({ ...formData, [e.target.name]: e.target.value });
    };

    const handleCardChange = (restaurant, e) => {
        const settings = getSettingsFor(restaurant);
        updateSettingsFor(restaurant, {
            cardData: { ...settings.cardData, [e.target.name]: e.target.value }
        });
    };

    const removeItem = (id) => {
        setCartItems(cartItems.filter(item => item.id !== id));
    };

    const groupedItems = cartItems.reduce((acc, item) => {
        if (!acc[item.restaurant]) acc[item.restaurant] = [];
        acc[item.restaurant].push(item);
        return acc;
    }, {});

    const getRestaurantSubtotal = (restaurant) => {
        return groupedItems[restaurant]?.reduce((sum, item) => sum + item.price * item.quantity, 0) || 0;
    };

    const getDeliveryCost = (paymentType) => paymentType === 'card' ? 50 : 0;

    const getRestaurantTotal = (restaurant) => {
        const settings = getSettingsFor(restaurant);
        return getRestaurantSubtotal(restaurant) + getDeliveryCost(settings.paymentType);
    };

    const getGrandTotal = () => {
        return Object.keys(groupedItems).reduce((sum, restaurant) => sum + getRestaurantTotal(restaurant), 0);
    };

    const handleSubmit = (e) => {
        e.preventDefault();
        if (!formData.name || !formData.phone) {
            alert('Будь ласка, заповніть імʼя та телефон');
            return;
        }
        for (const restaurant of Object.keys(groupedItems)) {
            const settings = getSettingsFor(restaurant);
            if (settings.deliveryType === 'delivery' && !settings.address) {
                alert(`Вкажіть адресу доставки для ${restaurant}`);
                return;
            }
            if (settings.paymentType === 'card') {
                const card = settings.cardData;
                if (!card.cardNumber || !card.cardExpiry || !card.cardCVV || !card.cardName) {
                    alert(`Заповніть дані карти для ${restaurant}`);
                    return;
                }
            }
        }
        alert('Усі замовлення успішно оформлено! Дякуємо 🎉');
    };

    return (
        <div className="app-wrapper">
            <CustomerSidebar />
            <div className="checkout-page-wrapper">
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

                <div className="checkout-container">
                    <motion.h1 initial={{ opacity: 0, y: -30 }} animate={{ opacity: 1, y: 0 }} className="checkout-title">
                        <CreditCard size={36} /> Оформлення замовлення
                    </motion.h1>

                    <form onSubmit={handleSubmit}>
                        {/* Контактна інформація */}
                        <motion.section initial={{ opacity: 0, y: 20 }} animate={{ opacity: 1, y: 0 }} className="checkout-section">
                            <h2><User size={28} /> Контактна інформація</h2>
                            <div className="form-grid">
                                <input type="text" name="name" placeholder="Ваше ім'я *" required value={formData.name} onChange={handleInputChange} />
                                <input type="tel" name="phone" placeholder="Телефон *" required value={formData.phone} onChange={handleInputChange} />
                                <input type="email" name="email" placeholder="Email (опціонально)" value={formData.email} onChange={handleInputChange} />
                            </div>
                        </motion.section>

                        {/* Блоки по ресторанах */}
                        {Object.entries(groupedItems).map(([restaurant, items], index) => {
                            const settings = getSettingsFor(restaurant);
                            return (
                                <motion.section key={restaurant} initial={{ opacity: 0, y: 30 }} animate={{ opacity: 1, y: 0 }} transition={{ delay: index * 0.1 }} className="checkout-section">
                                    <h2><ShoppingCart size={28} /> {restaurant}</h2>

                                    <div className="items-list">
                                        <AnimatePresence>
                                            {items.map((item) => (
                                                <motion.div key={item.id} layout initial={{ opacity: 0 }} animate={{ opacity: 1 }} exit={{ opacity: 0, x: -50 }} className="order-item">
                                                    <img src={item.image} alt={item.name} />
                                                    <div className="order-item-info">
                                                        <p className="name">{item.name}</p>
                                                        <p className="quantity">Кількість: {item.quantity}</p>
                                                    </div>
                                                    <div className="order-item-price">
                                                        <span>{item.price * item.quantity} ₴</span>
                                                        <button type="button" onClick={() => removeItem(item.id)} className="remove-btn">
                                                            <Trash2 size={20} />
                                                        </button>
                                                    </div>
                                                </motion.div>
                                            ))}
                                        </AnimatePresence>
                                    </div>

                                    <div className="delivery-payment-grid">
                                        <div>
                                            <h3><Truck size={22} /> Спосіб отримання</h3>
                                            <div className="options-group">
                                                <label className={settings.deliveryType === 'delivery' ? 'active' : ''}>
                                                    <input type="radio" checked={settings.deliveryType === 'delivery'} onChange={() => updateSettingsFor(restaurant, { deliveryType: 'delivery' })} />
                                                    <Truck size={22} /> Доставка
                                                </label>
                                                <label className={settings.deliveryType === 'pickup' ? 'active' : ''}>
                                                    <input type="radio" checked={settings.deliveryType === 'pickup'} onChange={() => updateSettingsFor(restaurant, { deliveryType: 'pickup' })} />
                                                    <Store size={22} /> Самовивіз
                                                </label>
                                            </div>
                                        </div>

                                        <div>
                                            <h3><CreditCard size={22} /> Спосіб оплати</h3>
                                            <div className="options-group">
                                                <label className={settings.paymentType === 'cash' ? 'active' : ''}>
                                                    <input type="radio" checked={settings.paymentType === 'cash'} onChange={() => updateSettingsFor(restaurant, { paymentType: 'cash' })} />
                                                    <DollarSign size={22} /> Готівкою при отриманні
                                                </label>
                                                <label className={settings.paymentType === 'card' ? 'active' : ''}>
                                                    <input type="radio" checked={settings.paymentType === 'card'} onChange={() => updateSettingsFor(restaurant, { paymentType: 'card' })} />
                                                    <CreditCard size={22} /> Карткою онлайн
                                                </label>
                                            </div>
                                        </div>
                                    </div>

                                    {settings.deliveryType === 'delivery' && (
                                        <div className="delivery-address-wrapper">
                                            <motion.input initial={{ opacity: 0 }} animate={{ opacity: 1 }} type="text" placeholder="Адреса доставки *" value={settings.address} onChange={(e) => updateSettingsFor(restaurant, { address: e.target.value })} required />
                                        </div>
                                    )}

                                    {settings.paymentType === 'card' && (
                                        <motion.div initial={{ opacity: 0 }} animate={{ opacity: 1 }} className="card-form">
                                            <input type="text" name="cardNumber" placeholder="Номер карти" value={settings.cardData.cardNumber} onChange={(e) => handleCardChange(restaurant, e)} required />
                                            <div className="card-row">
                                                <input type="text" name="cardExpiry" placeholder="MM/РР" value={settings.cardData.cardExpiry} onChange={(e) => handleCardChange(restaurant, e)} required />
                                                <input type="text" name="cardCVV" placeholder="CVV" value={settings.cardData.cardCVV} onChange={(e) => handleCardChange(restaurant, e)} required />
                                            </div>
                                            <input type="text" name="cardName" placeholder="Ім'я на карті" value={settings.cardData.cardName} onChange={(e) => handleCardChange(restaurant, e)} required />
                                        </motion.div>
                                    )}

                                    <div className="summary-block">
                                        <div className="summary-row"><span>Страви:</span> <strong>{getRestaurantSubtotal(restaurant)} ₴</strong></div>
                                        <div className="summary-row"><span>Доставка:</span> <strong>{getDeliveryCost(settings.paymentType)} ₴</strong></div>
                                        <div className="summary-row total"><span>До сплати:</span> <strong className="final-price">{getRestaurantTotal(restaurant)} ₴</strong></div>
                                    </div>
                                </motion.section>
                            );
                        })}

                        <motion.section initial={{ opacity: 0, y: 20 }} animate={{ opacity: 1, y: 0 }} className="checkout-section">
                            <h2>Коментар до замовлення (загальний)</h2>
                            <textarea name="comment" placeholder="Додаткові побажання" rows="5" value={formData.comment} onChange={handleInputChange} />
                        </motion.section>

                        <motion.section initial={{ opacity: 0, y: 30 }} animate={{ opacity: 1, y: 0 }} className="checkout-section total-section">
                            <div className="grand-total">
                                <span>Загальна сума до сплати:</span>
                                <strong className="grand-total-price">{getGrandTotal()} ₴</strong>
                            </div>
                            <button type="submit" className="submit-order-btn">
                                Оформити всі замовлення
                            </button>
                        </motion.section>
                    </form>

                    {/* Кнопки внизу — на одному рядку та відцентровані */}
                    <div className="bottom-links">
                        <Link to="/" className="continue-shopping-btn">
                            Продовжити покупки
                        </Link>
                        <Link to="/cart" className="back-to-cart">
                            <ArrowLeft size={20} /> Повернутись до кошика
                        </Link>
                    </div>
                </div>
            </div>
        </div>
    );
};

export default CheckoutPage;