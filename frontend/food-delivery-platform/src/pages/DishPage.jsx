// src/pages/DishPage.jsx
import React, { useState, useEffect } from 'react';
import { useParams, Link } from 'react-router-dom';
import { motion } from 'framer-motion';
import { Star, Clock, MapPin, ChevronLeft, Plus, Minus, Zap, MessageCircle } from 'lucide-react';
import './styles/DishPage.css';

const mockDishes = {
    1: {
        id: 1,
        name: "Маргарита Піца",
        image: "https://images.unsplash.com/photo-1565299624946-b28f40a0ae38?w=1200",
        rating: 4.8,
        reviews: 324,
        restaurant: "Pizza Palace",
        address: "вул. Італійська, 12, Київ",
        price: 249,
        oldPrice: 299,
        cookingTime: "25-30 хв",
        weight: "450 г",
        category: "Піца",
        popular: true,
        description: "Класична італійська піца з тонким тістом, томатним соусом, сиром моцарела та свіжим базиліком. Ідеальний вибір для любителів традицій.",
        ingredients: ["Тісто", "Томатний соус", "Моцарела", "Базилік свіжий", "Оливкова олія"],
        nutrition: { calories: 890, protein: 38, fats: 32, carbs: 112 },
        allergens: ["Глютен", "Молочні продукти"],
        reviewsList: [
            { id: 1, author: "Олена", rating: 5, text: "Найкраща Маргарита в місті! Тісто тонке, соус ідеальний.", date: "2 дні тому" },
            { id: 2, author: "Макс", rating: 4, text: "Дуже смачно, але хотілося б більше базиліку.", date: "1 тиждень тому" },
            { id: 3, author: "Софія", rating: 5, text: "Замовляю вже втретє. Рекомендую!", date: "1 місяць тому" }
        ]
    },
    2: {
        id: 2,
        name: "Бургер з яловичиною",
        image: "https://images.unsplash.com/photo-1568901346375-23c9450c58cd?w=1200",
        rating: 4.9,
        reviews: 287,
        restaurant: "Burger Hub",
        address: "просп. Перемоги, 45, Київ",
        price: 189,
        cookingTime: "15-20 хв",
        weight: "380 г",
        category: "Бургери",
        popular: true,
        description: "Соковитий бургер з 100% яловичини, свіжими овочами, сиром чеддер і фірмовим соусом.",
        ingredients: ["Булочка", "Яловичина", "Сир чеддер", "Салат", "Помідор", "Соус"],
        nutrition: { calories: 720, protein: 42, fats: 38, carbs: 56 },
        allergens: ["Глютен", "Молочні продукти"],
        reviewsList: [
            { id: 1, author: "Ігор", rating: 5, text: "Найсмачніший бургер! Соковитий і великий.", date: "3 дні тому" }
        ]
    },
    // Додай інші страви за потребою
};

const DishPage = () => {
    const { id } = useParams();
    const [dish, setDish] = useState(null);
    const [quantity, setQuantity] = useState(1);
    const [loading, setLoading] = useState(true);

    useEffect(() => {
        setTimeout(() => {
            setDish(mockDishes[id] || null);
            setLoading(false);
        }, 300);
    }, [id]);

    if (loading) {
        return (
            <div className="dish-loading" style={{ textAlign: 'center', padding: '4rem', color: '#ccc' }}>
                <div className="spinner" style={{
                    width: '50px',
                    height: '50px',
                    border: '5px solid #333',
                    borderTop: '5px solid #ff4500',
                    borderRadius: '50%',
                    animation: 'spin 1s linear infinite',
                    margin: '0 auto 1rem'
                }}></div>
                Завантаження...
            </div>
        );
    }

    if (!dish) {
        return (
            <div className="dish-not-found" style={{ textAlign: 'center', padding: '4rem', color: '#ccc' }}>
                <h2>Страву не знайдено</h2>
                <Link to="/" style={{ color: '#ff4500', textDecoration: 'none' }}>На головну</Link>
            </div>
        );
    }

    return (
        <div className="dish-page">
            <motion.div
                initial={{ opacity: 0, y: 20 }}
                animate={{ opacity: 1, y: 0 }}
                className="dish-container"
            >
                {/* Хлібні крихти */}
                <div className="breadcrumbs">
                    <Link to="/">Головна</Link> → <Link to="/">{dish.category}</Link> → <span>{dish.name}</span>
                </div>

                <div className="dish-grid">
                    {/* ЛІВИЙ БЛОК: Фото + Відгуки */}
                    <div className="dish-left">
                        <div className="dish-image-wrapper">
                            <img src={dish.image} alt={dish.name} className="dish-image" />
                            {dish.popular && <div className="popular-badge"><Zap size={16} /> ХІТ</div>}
                        </div>

                        {/* ВІДГУКИ */}
                        <div className="reviews-section">
                            <h3 className="reviews-title">
                                <MessageCircle size={20} /> Відгуки ({dish.reviews})
                            </h3>
                            <div className="reviews-list">
                                {dish.reviewsList.map(review => (
                                    <motion.div
                                        key={review.id}
                                        className="review-card"
                                        initial={{ opacity: 0, x: -20 }}
                                        animate={{ opacity: 1, x: 0 }}
                                        transition={{ delay: review.id * 0.1 }}
                                    >
                                        <div className="review-header">
                                            <strong>{review.author}</strong>
                                            <div className="review-rating">
                                                {[...Array(5)].map((_, i) => (
                                                    <Star
                                                        key={i}
                                                        size={14}
                                                        fill={i < review.rating ? "gold" : "none"}
                                                        stroke={i < review.rating ? "gold" : "#555"}
                                                    />
                                                ))}
                                            </div>
                                        </div>
                                        <p className="review-text">{review.text}</p>
                                        <small className="review-date">{review.date}</small>
                                    </motion.div>
                                ))}
                            </div>
                            <button className="show-more-reviews">
                                Показати всі відгуки →
                            </button>
                        </div>
                    </div>

                    {/* ПРАВИЙ БЛОК: Інформація */}
                    <div className="dish-info">
                        <h1 className="dish-title">{dish.name}</h1>

                        <div className="dish-meta">
                            <div className="rating">
                                <Star size={20} fill="gold" /> {dish.rating} ({dish.reviews} відгуків)
                            </div>
                            <div className="cooking-time">
                                <Clock size={18} /> {dish.cookingTime}
                            </div>
                        </div>

                        <div className="restaurant-info">
                            <MapPin size={16} /> <strong>{dish.restaurant}</strong><br />
                            <span className="address">{dish.address}</span>
                        </div>

                        <div className="price-section">
                            <div className="price-current">{dish.price} ₴</div>
                            {dish.oldPrice && <div className="price-old">{dish.oldPrice} ₴</div>}
                        </div>

                        <p className="description">{dish.description}</p>

                        {/* КІЛЬКІСТЬ — ВЕЛИКІ КНОПКИ + і – */}
                        <div className="quantity-controls">
                            <motion.button
                                whileHover={{ scale: 1.1 }}
                                whileTap={{ scale: 0.95 }}
                                onClick={() => setQuantity(q => Math.max(1, q - 1))}
                                className="qty-btn minus"
                                aria-label="Зменшити кількість"
                            >
                                <Minus size={28} strokeWidth={3} />
                            </motion.button>

                            <motion.span
                                className="quantity"
                                key={quantity}
                                initial={{ scale: 1.3 }}
                                animate={{ scale: 1 }}
                                transition={{ type: "spring", stiffness: 400, damping: 20 }}
                            >
                                {quantity}
                            </motion.span>

                            <motion.button
                                whileHover={{ scale: 1.1 }}
                                whileTap={{ scale: 0.95 }}
                                onClick={() => setQuantity(q => q + 1)}
                                className="qty-btn plus"
                                aria-label="Збільшити кількість"
                            >
                                <Plus size={28} strokeWidth={3} />
                            </motion.button>
                        </div>

                        <motion.button
                            whileHover={{ scale: 1.02 }}
                            whileTap={{ scale: 0.98 }}
                            className="add-to-cart-btn"
                        >
                            Додати в кошик · {dish.price * quantity} ₴
                        </motion.button>

                        {/* ДОДАТКОВА ІНФОРМАЦІЯ */}
                        <div className="extra-info">
                            {/* СКЛАД — ЗІ ЗІРОЧКАМИ * */}
                            <div className="info-block">
                                <h4>Склад</h4>
                                <ul className="ingredients-list">
                                    {dish.ingredients.map((ing, i) => (
                                        <li key={i}>{ing}</li>
                                    ))}
                                </ul>
                            </div>

                            <div className="info-block">
                                <h4>Харчова цінність (на 100г)</h4>
                                <div className="nutrition-grid">
                                    <div>Калорії: <strong>{dish.nutrition.calories} ккал</strong></div>
                                    <div>Білки: <strong>{dish.nutrition.protein} г</strong></div>
                                    <div>Жири: <strong>{dish.nutrition.fats} г</strong></div>
                                    <div>Вуглеводи: <strong>{dish.nutrition.carbs} г</strong></div>
                                </div>
                            </div>

                            {dish.allergens.length > 0 && (
                                <div className="info-block warning">
                                    <h4>Алергени</h4>
                                    <p>{dish.allergens.join(', ')}</p>
                                </div>
                            )}
                        </div>
                    </div>
                </div>

                {/* Повернення */}
                <Link to="/" className="back-to-menu">
                    <ChevronLeft size={18} /> Повернутися до меню
                </Link>
            </motion.div>
        </div>
    );
};

export default DishPage;