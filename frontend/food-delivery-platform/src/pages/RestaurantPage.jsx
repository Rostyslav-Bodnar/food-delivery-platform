// src/pages/RestaurantPage.jsx
import React from 'react';
import { useParams, Link } from 'react-router-dom';
import { motion } from 'framer-motion';
import { ArrowLeft } from 'lucide-react';
import './styles/restaurantPage.css';

// ← ВСІ ДАНІ ТУТ, без mockData.js
const mockDishes = {
    1: {
        id: 1,
        name: "Маргарита Піца",
        image: "https://images.unsplash.com/photo-1565299624946-b28f40a0ae38?w=1200",
        restaurant: "Pizza Palace",
        price: 249,
    },
    2: {
        id: 2,
        name: "Бургер з яловичиною",
        image: "https://images.unsplash.com/photo-1568901346375-23c9450c58cd?w=1200",
        restaurant: "Burger Hub",
        price: 189,
    },
    3: {
        id: 3,
        name: "Пепероні",
        image: "https://images.unsplash.com/photo-1628840042765-0a9e5c3c8d5f?w=1200",
        restaurant: "Pizza Palace",
        price: 269,
    },
    4: {
        id: 4,
        name: "Бекон Бургер",
        image: "https://images.unsplash.com/photo-1553979459-d2229ba7433b?w=1200",
        restaurant: "Burger Hub",
        price: 229,
    },
    5: {
        id: 5,
        name: "Філадельфія Класик",
        image: "https://images.unsplash.com/photo-1579584425555-c3ce17fd4351?w=1200",
        restaurant: "Sushi Master",
        price: 329,
    },
    // Додавай ще скільки хочеш
};

const restaurants = {
    1: { id: 1, name: "Pizza Palace", banner: "https://images.unsplash.com/photo-1604382354936-07adb7bb5b25?w=1600" },
    2: { id: 2, name: "Burger Hub", banner: "https://images.unsplash.com/photo-1553979459-d2229ba7433b?w=1600" },
    3: { id: 3, name: "Sushi Master", banner: "https://images.unsplash.com/photo-1579751626657-72bc1701048f?w=1600" },
};

const RestaurantPage = () => {
    const { id } = useParams();
    const restaurant = restaurants[id];

    // Отримуємо страви тільки цього ресторану
    const restaurantDishes = Object.values(mockDishes).filter(
        dish => dish.restaurant === restaurant?.name
    );

    if (!restaurant) {
        return <div style={{ textAlign: 'center', padding: '100px', color: '#ccc' }}>Ресторан не знайдено</div>;
    }

    return (
        <div className="restaurant-page-wrapper">
            {/* Частинки */}
            <div className="particles">
                {[...Array(6)].map((_, i) => (
                    <motion.div
                        key={i}
                        className="particle"
                        initial={{ y: -100 }}
                        animate={{ y: window.innerHeight + 100 }}
                        transition={{ duration: 20 + i * 4, repeat: Infinity, ease: "linear" }}
                    />
                ))}
            </div>

            {/* Банер */}
            <div className="restaurant-banner" style={{ backgroundImage: `url(${restaurant.banner})` }}>
                <div className="banner-overlay">
                    <Link to="/restaurants" className="back-btn">
                        <ArrowLeft size={28} /> Назад до закладів
                    </Link>
                    <motion.h1
                        initial={{ opacity: 0, y: 40 }}
                        animate={{ opacity: 1, y: 0 }}
                        className="restaurant-title"
                    >
                        {restaurant.name}
                    </motion.h1>
                </div>
            </div>

            {/* Меню — перехід на твою DishPage */}
            <div className="restaurant-container">
                <motion.div className="menu-grid">
                    {restaurantDishes.map((dish, i) => (
                        <motion.div
                            key={dish.id}
                            initial={{ opacity: 0, y: 30 }}
                            animate={{ opacity: 1, y: 0 }}
                            transition={{ delay: i * 0.1 }}
                            whileHover={{ y: -12 }}
                        >
                            <Link to={`/dish/${dish.id}`} className="dish-link">
                                <div className="dish-card">
                                    <img src={dish.image} alt={dish.name} />
                                    <div className="dish-info">
                                        <h3>{dish.name}</h3>
                                        <div className="dish-bottom">
                                            <span className="price">{dish.price} ₴</span>
                                        </div>
                                    </div>
                                </div>
                            </Link>
                        </motion.div>
                    ))}
                </motion.div>
            </div>
        </div>
    );
};

export default RestaurantPage;