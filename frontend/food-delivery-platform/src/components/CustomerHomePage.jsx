import React, { useState, useEffect } from 'react';
import { Link } from 'react-router-dom';
import { motion, AnimatePresence } from 'framer-motion';
import {
    Search, Filter, Star, MapPin, ShoppingCart,
    ChevronLeft, ChevronRight, X, Zap, Home, Store, User, Package
} from 'lucide-react';
import './styles/CustomerHomePage.css';
import { getAllDishesForCustomer} from "../api/Dish.jsx";
import CustomerSidebar from './sidebars/CustomerSidebar';
import DishCardComponent from './customer-components/DishCardComponent';
import {CategoryList, CategoryMap} from "../constants/category.jsx";

const CustomerHomePage = () => {
    const [popularDishes, setPopularDishes] = useState([]);
    const [allDishes, setAllDishes] = useState([]);
    const [filteredDishes, setFilteredDishes] = useState([]);
    const [searchTerm, setSearchTerm] = useState('');
    const [selectedCategory, setSelectedCategory] = useState('all');
    const [selectedRating, setSelectedRating] = useState('all');
    const [priceRange, setPriceRange] = useState([0, 5000]);
    const [loading, setLoading] = useState(true);
    const [isFilterOpen, setIsFilterOpen] = useState(false);

    useEffect(() => {
        const fetchDishes = async () => {
            try {
                // 1. Залишаємось з мок-даними ДЛЯ ТОР-ХІТІВ
                const mockDishes = [
                    { id: 1, name: "Маргарита Піца", image: "...", rating: 4.8, restaurant: "Pizza Palace", price: 249, category: "pizza", popular: true },
                    { id: 2, name: "Бургер з яловичиною", image: "...", rating: 4.9, restaurant: "Burger Hub", price: 189, category: "burger", popular: true },
                    { id: 3, name: "Суші Сет Дракон", image: "...", rating: 4.7, restaurant: "Sushi Master", price: 429, category: "sushi", popular: true },
                ];

                // 2. Додаємо мок-дані як топ
                const topDishes = mockDishes
                    .filter(d => d.popular)
                    .sort((a, b) => b.rating - a.rating);

                setPopularDishes(topDishes);

                // 3. Завантажуємо реальні страви з бекенду
                setLoading(true);
                debugger;

                const data = await getAllDishesForCustomer();
                // Перетворення у формат, з яким працює фронт
                const mappedDishes = data.map(d => ({
                    id: d.id,
                    name: d.name,
                    image: d.imageUrl,
                    rating: 4.5, // якщо поки немає рейтингу з бекенду
                    restaurant: d.businessDetails.name, // бекенд не дає назву закладу
                    price: d.price,
                    category: CategoryMap[d.category]
                }));

                setAllDishes(mappedDishes);
                setFilteredDishes(mappedDishes);
            }
            catch (err) {
                console.error("Помилка при завантаженні страв:", err);
            }
            finally {
                setLoading(false);
            }
        };

        fetchDishes();
    }, []);

    useEffect(() => {
        let filtered = allDishes;
        if (searchTerm) {
            filtered = filtered.filter(d =>
                d.name.toLowerCase().includes(searchTerm.toLowerCase()) ||
                d.restaurant.toLowerCase().includes(searchTerm.toLowerCase())
            );
        }
        if (selectedCategory !== 'all') filtered = filtered.filter(d => d.category === selectedCategory);
        if (selectedRating !== 'all') filtered = filtered.filter(d => d.rating >= parseFloat(selectedRating));
        filtered = filtered.filter(d => d.price >= priceRange[0] && d.price <= priceRange[1]);
        setFilteredDishes(filtered);
    }, [searchTerm, selectedCategory, selectedRating, priceRange, allDishes]);

    const scrollPopular = (direction) => {
        const container = document.querySelector('.popular-scroll');
        const scrollAmount = 340;
        container.scrollBy({ left: direction === 'left' ? -scrollAmount : scrollAmount, behavior: 'smooth' });
    };

    return (
        <div className="app-wrapper">
            {/* МЕНЮ ЗЛІВА — просто додано, нічого не зламано */}
            <CustomerSidebar />


            {/* ТВІЙ ОРИГІНАЛЬНИЙ КОНТЕНТ — 1 в 1 */}
            <div className="auth-homepage">
                <div className="particles">
                    {[...Array(6)].map((_, i) => (
                        <motion.div
                            key={i}
                            className="particle"
                            initial={{ y: -100, x: Math.random() * window.innerWidth }}
                            animate={{ y: window.innerHeight + 100 }}
                            transition={{
                                duration: 15 + Math.random() * 10,
                                repeat: Infinity,
                                ease: "linear",
                                delay: Math.random() * 5
                            }}
                        />
                    ))}
                </div>

                <motion.section className="search-hero" initial={{ opacity: 0, y: 20 }} animate={{ opacity: 1, y: 0 }} transition={{ delay: 0.2 }}>
                    <div className="search-container">
                        <Search size={24} className="search-icon" />
                        <input
                            type="text"
                            placeholder="Що бажаєте поїсти?.."
                            value={searchTerm}
                            onChange={(e) => setSearchTerm(e.target.value)}
                            className="search-input"
                        />
                        {searchTerm && (
                            <motion.div initial={{ opacity: 0 }} animate={{ opacity: 1 }} className="search-suggestions">
                                <p>Пошук: <strong>"{searchTerm}"</strong> — знайдено {filteredDishes.length} страв</p>
                            </motion.div>
                        )}
                    </div>
                    <motion.button whileHover={{ scale: 1.05 }} whileTap={{ scale: 0.95 }} onClick={() => setIsFilterOpen(true)} className="filter-toggle">
                        <Filter size={20} /> Фільтри
                    </motion.button>
                </motion.section>

                <AnimatePresence>
                    {isFilterOpen && (
                        <>
                            <motion.aside className="filter-sidebar" initial={{ x: -350 }} animate={{ x: 0 }} exit={{ x: -350 }} transition={{ type: "spring", stiffness: 300, damping: 30 }}>
                                <div className="filter-header">
                                    <h3>Фільтри</h3>
                                    <motion.button whileHover={{ scale: 1.1 }} whileTap={{ scale: 0.9 }} onClick={() => setIsFilterOpen(false)} className="close-btn">
                                        <X size={26} />
                                    </motion.button>
                                </div>
                                <motion.div className="filter-group" initial={{ opacity: 0 }} animate={{ opacity: 1 }} transition={{ delay: 0.1 }}>
                                    <label>Категорія</label>
                                    <select value={selectedCategory} onChange={(e) => setSelectedCategory(e.target.value)}>
                                        <option value="all">Всі страви</option>
                                        {CategoryList.map(cat => (
                                            <option key={cat.id} value={cat.name}>
                                                {cat.name}
                                            </option>
                                        ))}
                                    </select>

                                </motion.div>
                                <motion.div className="filter-group" initial={{ opacity: 0 }} animate={{ opacity: 1 }} transition={{ delay: 0.15 }}>
                                    <label>Мінімальний рейтинг</label>
                                    <select value={selectedRating} onChange={(e) => setSelectedRating(e.target.value)}>
                                        <option value="all">Без обмежень</option>
                                        <option value="4.5">4.5+</option>
                                        <option value="4.0">4.0+</option>
                                        <option value="3.5">3.5+</option>
                                    </select>
                                </motion.div>
                                <motion.div className="filter-group" initial={{ opacity: 0 }} animate={{ opacity: 1 }} transition={{ delay: 0.2 }}>
                                    <label>Ціна: {priceRange[0]}₴ — {priceRange[1]}₴</label>
                                    <div className="range-container">
                                        <div className="range-label">
                                            <span>{priceRange[0]}₴</span>
                                            <span>{priceRange[1]}₴</span>
                                        </div>
                                        <input type="range" min="0" max="5000" value={priceRange[1]}
                                            onChange={(e) => setPriceRange([priceRange[0], parseInt(e.target.value)])}
                                            className="range-slider" />
                                    </div>
                                </motion.div>
                                <motion.button whileHover={{ scale: 1.03 }} whileTap={{ scale: 0.98 }} onClick={() => setIsFilterOpen(false)} className="apply-filters-btn">
                                    Показати {filteredDishes.length} страв
                                </motion.button>
                            </motion.aside>
                            <motion.div className="filter-overlay" initial={{ opacity: 0 }} animate={{ opacity: 1 }} exit={{ opacity: 0 }} onClick={() => setIsFilterOpen(false)} />
                        </>
                    )}
                </AnimatePresence>

                <motion.section className="popular-section" initial={{ opacity: 0 }} whileInView={{ opacity: 1 }} viewport={{ once: true }} transition={{ delay: 0.3 }}>
                    <div className="section-header">
                        <h2 className="gradient-title">Топ-страви дня</h2>
                        <div className="scroll-controls">
                            <motion.button whileHover={{ scale: 1.1 }} whileTap={{ scale: 0.9 }} onClick={() => scrollPopular('left')} className="scroll-btn">
                                <ChevronLeft />
                            </motion.button>
                            <motion.button whileHover={{ scale: 1.1 }} whileTap={{ scale: 0.9 }} onClick={() => scrollPopular('right')} className="scroll-btn">
                                <ChevronRight />
                            </motion.button>
                        </div>
                    </div>
                    <div className="popular-scroll">
                        {popularDishes.map((dish, index) => (
                            <motion.div key={dish.id} initial={{ opacity: 0, y: 50 }} whileInView={{ opacity: 1, y: 0 }} viewport={{ once: true }} transition={{ delay: index * 0.1 }}>
                                <Link to={`/dish/${dish.id}`} className="dish-card popular-card">
                                    <div className="image-wrapper">
                                        <img src={dish.image} alt={dish.name} />
                                        <div className="rating-badge"><Star size={16} fill="gold" /> {dish.rating}</div>
                                        {dish.popular && <div className="popular-tag"><Zap size={14} /> ХІТ</div>}
                                    </div>
                                    <div className="dish-info">
                                        <h3>{dish.name}</h3>
                                        <p className="restaurant"><MapPin size={14} /> {dish.restaurant}</p>
                                        <div className="price-tag">{dish.price} ₴</div>
                                    </div>
                                </Link>
                            </motion.div>
                        ))}
                    </div>
                </motion.section>

                <motion.section className="dishes-section" initial={{ opacity: 0 }} whileInView={{ opacity: 1 }} viewport={{ once: true }}>
                    <h2 className="section-title">Усі страви</h2>
                    {loading ? (
                        <div className="skeleton-grid">
                            {[...Array(6)].map((_, i) => (
                                <motion.div key={i} className="skeleton-card" initial={{ opacity: 0 }} animate={{ opacity: 1 }} transition={{ delay: i * 0.1 }} />
                            ))}
                        </div>
                    ) : filteredDishes.length === 0 ? (
                        <motion.p initial={{ opacity: 0, y: 20 }} animate={{ opacity: 1, y: 0 }} className="no-results">
                            Нічого не знайдено
                        </motion.p>
                    ) : (
                        <motion.div className="dishes-grid">
                            {filteredDishes.map((dish, index) => (
                                <motion.div key={dish.id} initial={{ opacity: 0, scale: 0.9 }} whileInView={{ opacity: 1, scale: 1 }} viewport={{ once: true }} transition={{ delay: index * 0.05 }} whileHover={{ y: -8 }}>
                                    <DishCardComponent dish={dish} />
                                </motion.div>
                            ))}
                        </motion.div>
                    )}
                </motion.section>
            </div>
        </div>
    );
};

export default CustomerHomePage;