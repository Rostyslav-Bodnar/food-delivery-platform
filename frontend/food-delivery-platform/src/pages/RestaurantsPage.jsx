import React, { useState, useMemo } from 'react';
import { Link } from 'react-router-dom';
import { motion, AnimatePresence } from 'framer-motion';
import { Store, Search, X, Filter, ChevronRight, Star, Clock, MapPin } from 'lucide-react';
import './styles/restaurantsPage.css';
import CustomerSidebar from "../components/customer-home-page-components/CustomerSidebar.jsx";

const RestaurantsPage = () => {
    const [searchQuery, setSearchQuery] = useState('');
    const [selectedCategory, setSelectedCategory] = useState('all');
    const [sortBy, setSortBy] = useState('rating');

    const restaurants = [
        { id: 1, name: "Pizza Palace", rating: 4.8, deliveryTime: "25-35 хв", deliveryPrice: "Безкоштовно", image: "https://images.unsplash.com/photo-1517248135467-4c7edcad34c4?w=800", category: "Піца • Італійська" },
        { id: 2, name: "Burger Hub", rating: 4.9, deliveryTime: "20-30 хв", deliveryPrice: "59 ₴", image: "https://images.unsplash.com/photo-1571091718767-18b5b1457add?w=800", category: "Бургери • Американська" },
        { id: 3, name: "Sushi Master", rating: 4.7, deliveryTime: "35-45 хв", deliveryPrice: "Безкоштовно від 500₴", image: "https://images.unsplash.com/photo-1579584425555-c3ce17fd4351?w=800", category: "Суші • Японська" },
        { id: 4, name: "La Pasta", rating: 4.6, deliveryTime: "30-40 хв", deliveryPrice: "Безкоштовно", image: "https://images.unsplash.com/photo-1621996345872-7a8d9b2e7d3e?w=800", category: "Паста • Італійська" },
        { id: 5, name: "Green Bowl", rating: 4.8, deliveryTime: "20-30 хв", deliveryPrice: "49 ₴", image: "https://images.unsplash.com/photo-1512621776951-a57141f2eefd?w=800", category: "Салати • Здорове харчування" },
        { id: 6, name: "Shawarma King", rating: 4.5, deliveryTime: "15-25 хв", deliveryPrice: "39 ₴", image: "https://images.unsplash.com/photo-1626074353765-517a681e40be?w=800", category: "Шаурма • Стрітфуд" },
        { id: 7, name: "Wok House", rating: 4.7, deliveryTime: "30-40 хв", deliveryPrice: "Безкоштовно", image: "https://images.unsplash.com/photo-1512058564366-1450e26389be?w=800", category: "Азійська • Wok" },
        { id: 8, name: "Sweet Dreams", rating: 4.9, deliveryTime: "40-50 хв", deliveryPrice: "79 ₴", image: "https://images.unsplash.com/photo-1558326567-98ae2405596b?w=800", category: "Десерти • Торти" },
    ];

    const filteredAndSorted = useMemo(() => {
        let result = [...restaurants];
        if (searchQuery) {
            result = result.filter(r =>
                r.name.toLowerCase().includes(searchQuery.toLowerCase())
            );
        }
        if (selectedCategory !== 'all') {
            result = result.filter(r => r.category.includes(selectedCategory));
        }
        result.sort((a, b) => {
            if (sortBy === 'rating') return b.rating - a.rating;
            if (sortBy === 'time') {
                const timeA = parseInt(a.deliveryTime);
                const timeB = parseInt(b.deliveryTime);
                return timeA - timeB;
            }
            if (sortBy === 'name') return a.name.localeCompare(b.name);
            return 0;
        });
        return result;
    }, [searchQuery, selectedCategory, sortBy]);

    return (
        <div className="app-wrapper">
            {/* ЛІВА ПАНЕЛЬ */}
            <CustomerSidebar />

            {/* ПРАВИЙ КОНТЕНТ */}
            <div className="main-content">
                {/* Частинки (по всьому екрану) */}
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
                    {/* Заголовок + Пошук + Фільтри */}
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
                                        <option value="Піца">Піца</option>
                                        <option value="Бургери">Бургери</option>
                                        <option value="Суші">Суші</option>
                                        <option value="Паста">Паста</option>
                                        <option value="Салати">Салати</option>
                                        <option value="Шаурма">Шаурма</option>
                                        <option value="Азійська">Азійська</option>
                                        <option value="Десерти">Десерти</option>
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

                    {/* СІТКА ЗАКЛАДІВ */}
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
                                {filteredAndSorted.map((restaurant, index) => (
                                    <motion.div
                                        key={restaurant.id}
                                        layout
                                        initial={{ opacity: 0, scale: 0.9 }}
                                        animate={{ opacity: 1, scale: 1 }}
                                        exit={{ opacity: 0, scale: 0.9 }}
                                        transition={{ delay: index * 0.05 }}
                                        whileHover={{ y: -12, scale: 1.03 }}
                                        className="restaurant-card"
                                    >
                                        <Link to={`/restaurant/${restaurant.id}`} className="restaurant-link">
                                            <div className="restaurant-image-wrapper">
                                                <img src={restaurant.image} alt={restaurant.name} />
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
                                                <p className="restaurant-category">{restaurant.category}</p>
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