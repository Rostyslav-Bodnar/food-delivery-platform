// src/pages/CheckoutPage.jsx
import React, { useState, useEffect } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { motion, AnimatePresence } from 'framer-motion';
import {
    ShoppingCart, User, Truck, Store, CreditCard,
    DollarSign, Trash2, ArrowLeft
} from 'lucide-react';

import "./styles/CheckoutPage.css";
import CustomerSidebar from "../components/sidebars/CustomerSidebar.jsx";
import { createOrders } from "../api/Order.jsx";

import { MapContainer, TileLayer, Marker, useMapEvents } from 'react-leaflet';
import 'leaflet/dist/leaflet.css';
import L from 'leaflet';

// utils localStorage
const getCart = () => JSON.parse(localStorage.getItem("cart")) || [];
const clearCart = () => localStorage.removeItem("cart");

const CheckoutPage = () => {
    const navigate = useNavigate();
    const [mapPosition, setMapPosition] = useState(null); // {lat, lng}
    const [mapAddress, setMapAddress] = useState('');

    const [cartItems, setCartItems] = useState([]);
    const [formData, setFormData] = useState({
        name: '', phone: '', email: '', comment: ''
    });
    const [restaurantSettings, setRestaurantSettings] = useState({});

    useEffect(() => {
        setCartItems(getCart());
    }, []);

    const getSettingsFor = (restaurant) => {
        if (!restaurantSettings[restaurant]) {
            return {
                deliveryType: 'delivery',
                paymentType: 'cash',
                address: '',
                cardData: { cardNumber: '', cardExpiry: '', cardCVV: '', cardName: '' }
            };
        }
        console.log(restaurantSettings[restaurant]);
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
        const updated = cartItems.filter(item => item.id !== id);
        setCartItems(updated);
        localStorage.setItem("cart", JSON.stringify(updated));
    };

    // 🔹 групування по ресторанах
    const groupedItems = cartItems.reduce((acc, item) => {
        if (!acc[item.restaurant]) acc[item.restaurant] = [];
        acc[item.restaurant].push(item);
        return acc;
    }, {});

    const getRestaurantSubtotal = (restaurant) =>
        groupedItems[restaurant]?.reduce((sum, i) => sum + i.price * i.quantity, 0) || 0;

    const getDeliveryCost = (paymentType) => paymentType === 'card' ? 50 : 0;

    const getRestaurantTotal = (restaurant) => {
        const settings = getSettingsFor(restaurant);
        return getRestaurantSubtotal(restaurant) + getDeliveryCost(settings.paymentType);
    };

    const getGrandTotal = () =>
        Object.keys(groupedItems).reduce((sum, r) => sum + getRestaurantTotal(r), 0);

    // всередині CheckoutPage.jsx
    const LocationPicker = ({ position, setPosition }) => {
        useMapEvents({
            click(e) {
                setPosition(e.latlng);
            },
        });

        return position === null ? null : (
            <Marker position={position} />
        );
    };

    const fetchAddressFromCoords = async ({ lat, lng }) => {
        try {
            const res = await fetch(`https://nominatim.openstreetmap.org/reverse?format=json&lat=${lat}&lon=${lng}`);
            const data = await res.json();
            if (data.display_name) setMapAddress(data.display_name);
        } catch (err) {
            console.error("Помилка отримання адреси з координатів:", err);
        }
    };

    useEffect(() => {
        if (mapPosition) fetchAddressFromCoords(mapPosition);
    }, [mapPosition]);



    // 🔥 ОСНОВНЕ — звʼязок з бекендом
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
                                            <motion.input
                                                initial={{ opacity: 0 }}
                                                animate={{ opacity: 1 }}
                                                type="text"
                                                placeholder="Адреса доставки *"
                                                value={settings.address || mapAddress}
                                                onChange={(e) => updateSettingsFor(restaurant, { address: e.target.value })}
                                                required
                                            />

                                            <MapContainer center={[50.45, 30.52]} zoom={12} className="leaflet-container">
                                                <TileLayer
                                                    url="https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png"
                                                    attribution="&copy; OpenStreetMap contributors"
                                                />
                                                <LocationPicker position={mapPosition} setPosition={setMapPosition} />
                                            </MapContainer>
                                            <small>Клікніть на карті, щоб вибрати місце доставки</small>
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