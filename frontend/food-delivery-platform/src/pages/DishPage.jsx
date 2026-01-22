// src/pages/DishPage.jsx
import React, { useState, useEffect } from 'react';
import { useParams, Link } from 'react-router-dom';
import { motion } from 'framer-motion';
import { Star, Clock, MapPin, ChevronLeft, Plus, Minus } from 'lucide-react';
import './styles/DishPage.css';
import {getDishForCustomer} from "../api/Dish.jsx";
import { addToCart } from "../utils/CartStorage.jsx";
import DishLeftColumn from "../components/dish/DishLeftColumn";
import DishRightColumn from "../components/dish/DishRightColumn";


// МОК ВІДГУКИ — ЗАЛИШАЮТЬСЯ
const mockReviews = {
    1: [
        { id: 1, author: "Олена", rating: 5, text: "Найкраща Маргарита!", date: "2 дні тому" },
        { id: 2, author: "Макс", rating: 4, text: "Хотілося б більше базиліку.", date: "1 тиждень тому" },
    ],
    2: [
        { id: 1, author: "Ігор", rating: 5, text: "Соковитий і великий бургер.", date: "3 дні тому" }
    ]
};

const DishPage = () => {
    const { id } = useParams();
    const [dish, setDish] = useState(null);
    const [loading, setLoading] = useState(true);
    const [quantity, setQuantity] = useState(1);

    useEffect(() => {
        const loadDish = async () => {
            try {
                const data = await getDishForCustomer(id);

                const mappedDish = {
                    id: data.id,
                    name: data.name,
                    businessId: data.businessDetails.id,
                    image: data.imageUrl,
                    price: data.price,
                    cookingTime: data.cookingTime ? `${data.cookingTime} хв` : "",
                    description: data.description ?? "Опис відсутній",
                    category: data.categoryName ?? "Страва",
                    weight: "300 г",
                    rating: 4.5,               // бекенд поки не дає рейтинг
                    reviewsCount: mockReviews[id]?.length ?? 0,
                    restaurant: data.businessDetails.name,
                    popular: true,
                    ingredients: data.ingredients?.map(i => i.name) ?? [],
                    nutrition: { calories: 400, protein: 20, fats: 10, carbs: 60 },
                    allergens: ["Глютен"]
                };

                setDish(mappedDish);
            } catch (err) {
                console.error("Помилка завантаження страви:", err);
            } finally {
                setLoading(false);
            }
        };

        loadDish();
    }, [id]);

    if (loading) {
        return (
            <div className="dish-loading">
                <div className="spinner"></div>
                Завантаження...
            </div>
        );
    }

    if (!dish) {
        return (
            <div className="dish-not-found">
                <h2>Страву не знайдено</h2>
                <Link to="/">На головну</Link>
            </div>
        );
    }

    const reviewsList = mockReviews[id] ?? [];

    const handleAddToCart = () => {
        addToCart(dish, quantity);
    };


    return (
        <div className="dish-page">
            <motion.div initial={{ opacity: 0, y: 20 }} animate={{ opacity: 1, y: 0 }} className="dish-container">

                {/* Хлібні крихти */}
                <div className="breadcrumbs">
                    <Link to="/">Головна</Link> → <span>{dish.category}</span> → {dish.name}
                </div>

                <div className="dish-grid">
                    {/* ЛІВА КОЛОНКА — Фото + Відгуки */}
                    <DishLeftColumn
                        dish={dish}
                        reviewsList={reviewsList}
                    />

                    {/* ПРАВА КОЛОНКА — Інформація */}
                    <DishRightColumn
                        dish={dish}
                        quantity={quantity}
                        setQuantity={setQuantity}
                        handleAddToCart={handleAddToCart}
                    />
                </div>

                <Link to="/" className="back-to-menu">
                    <ChevronLeft size={18} /> На головну
                </Link>
            </motion.div>
        </div>
    );
};

export default DishPage;
