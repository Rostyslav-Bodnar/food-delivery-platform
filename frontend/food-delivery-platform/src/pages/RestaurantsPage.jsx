import React, { useState, useMemo, useEffect } from 'react';
import { Link } from 'react-router-dom';
import { motion, AnimatePresence } from 'framer-motion';
import { Store, Search, X, Filter, ChevronRight, Star, Clock, MapPin } from 'lucide-react';
import './styles/RestaurantsPage.css';
import CustomerSidebar from "../components/customer-components/CustomerSidebar.jsx";
import { getAllBusinessAccounts } from "../api/Account.jsx";

const RestaurantsPage = () => {
    const [restaurants, setRestaurants] = useState([]);
    const [searchQuery, setSearchQuery] = useState('');
    const [selectedCategory, setSelectedCategory] = useState('all');
    const [sortBy, setSortBy] = useState('rating');

    useEffect(() => {
        const fetchRestaurants = async () => {
            try {
                const response = await getAllBusinessAccounts();
                setRestaurants(
                    response.map(r => ({
                        id: r.id,
                        name: r.name,
                        image: r.imageUrl,
                        description: r.description,
                        // бо бек поки не повертає рейтинг/доставку — ставимо заглушки
                        rating: 4.8,
                        deliveryTime: "25-40 хв",
                        deliveryPrice: "Безкоштовно",
                        category: r.description ?? "Ресторан"
                    }))
                );
            } catch (e) {
                console.error("Помилка отримання бізнес акаунтів:", e);
            }
        };
        fetchRestaurants();
    }, []);

    const filteredAndSorted = useMemo(() => {
        let result = [...restaurants];

        if (searchQuery) {
            result = result.filter(r =>
                r.name.toLowerCase().includes(searchQuery.toLowerCase())
            );
        }

        if (selectedCategory !== 'all') {
            result = result.filter(r =>
                r.category?.toLowerCase().includes(selectedCategory.toLowerCase())
            );
        }

        result.sort((a, b) => {
            if (sortBy === 'rating') return b.rating - a.rating;
            if (sortBy === 'time') return parseInt(a.deliveryTime) - parseInt(b.deliveryTime);
            if (sortBy === 'name') return a.name.localeCompare(b.name);
            return 0;
        });

        return result;
    }, [searchQuery, selectedCategory, sortBy, restaurants]);

    return (
        <div className="app-wrapper">
            <CustomerSidebar />

            <div className="main-content">
                <div className="particles">
                    {[...Array(8)].map((_, i) => (
                        <motion.div
                            key={i}
                            className="particle"
                            initial={{ y: -100 }}
                            animate={{ y: window.innerHeight + 100 }}
                            transition={{
                                duration: 15 + Math.random() * 15,
                                repeat: Infinity,
                                ease: "linear",
                                delay: Math.random() * 5
                            }}
                        />
                    ))}
                </div>

                <div className="restaurants-container">
                    <motion.div
                        initial={{ opacity: 0, y: -30 }}
                        animate={{ opacity: 1, y: 0 }}
                        className="controls-header"
                    >
                        <h1 className="page-title">
                            <Store size={40} /> Усі заклади
                        </h1>

                        <div className="search-and-filters">
                            <div className="search-bar">
                                <Search size={20} className="search-icon" />
                                <input
                                    type="text"
                                    placeholder="Пошук закладу..."
                                    value={searchQuery}
                                    onChange={(e) => setSearchQuery(e.target.value)}
                                />
                                {searchQuery && (
                                    <X size={20} className="clear-search" onClick={() => setSearchQuery('')} />
                                )}
                            </div>

                            <div className="filters-group">
                                <div className="filter-item">
                                    <Filter size={18} />
                                    <select value={selectedCategory} onChange={(e) => setSelectedCategory(e.target.value)}>
                                        <option value="all">Всі категорії</option>
                                        <option value="pizza">Піца</option>
                                        <option value="burgers">Бургери</option>
                                        <option value="sushi">Суші</option>
                                        <option value="pasta">Паста</option>
                                        <option value="salads">Салати</option>
                                        <option value="shawarma">Шаурма</option>
                                        <option value="asian">Азійська</option>
                                        <option value="desserts">Десерти</option>
                                    </select>
                                </div>

                                <div className="filter-item">
                                    <select value={sortBy} onChange={(e) => setSortBy(e.target.value)}>
                                        <option value="rating">За рейтингом</option>
                                        <option value="time">Швидка доставка</option>
                                        <option value="name">За назвою</option>
                                    </select>
                                </div>
                            </div>
                        </div>
                    </motion.div>

                    <AnimatePresence mode="wait">
                        {filteredAndSorted.length === 0 ? (
                            <motion.div
                                key="no-results"
                                initial={{ opacity: 0 }}
                                animate={{ opacity: 1 }}
                                className="no-results"
                            >
                                <p>Нічого не знайдено</p>
                            </motion.div>
                        ) : (
                            <motion.div className="restaurants-grid">
                                {filteredAndSorted.map((restaurant) => (
                                    <motion.div
                                        key={restaurant.id}
                                        layout
                                        initial={{ opacity: 0, scale: 0.9 }}
                                        animate={{ opacity: 1, scale: 1 }}
                                        exit={{ opacity: 0, scale: 0.9 }}
                                        transition={{ duration: 0.2 }}
                                        whileHover={{ y: -12, scale: 1.03 }}
                                        className="restaurant-card"
                                    >
                                        <Link
                                            to={`/restaurant/${restaurant.id}`}
                                            className="restaurant-link"
                                            state={{ restaurant }}
                                        >
                                        <div className="restaurant-image-wrapper">
                                                <img
                                                    src={restaurant.image || "https://via.placeholder.com/400x250?text=No+Image"}
                                                    alt={restaurant.name}
                                                />
                                                <div className="restaurant-overlay">
                                                    <ChevronRight size={30} />
                                                </div>
                                            </div>

                                            <div className="restaurant-info">
                                                <div className="restaurant-header">
                                                    <h3>{restaurant.name}</h3>
                                                    <div className="rating-badge">
                                                        <Star size={16} fill="gold" /> {restaurant.rating}
                                                    </div>
                                                </div>

                                                <p className="restaurant-category">
                                                    {restaurant.category}
                                                </p>

                                                <div className="restaurant-details">
                                                    <span><Clock size={15} /> {restaurant.deliveryTime}</span>
                                                    <span><MapPin size={15} /> {restaurant.deliveryPrice}</span>
                                                </div>
                                            </div>
                                        </Link>
                                    </motion.div>
                                ))}
                            </motion.div>
                        )}
                    </AnimatePresence>
                </div>
            </div>
        </div>
    );
};

export default RestaurantsPage;
