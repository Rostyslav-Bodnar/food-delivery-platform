// src/pages/RestaurantPage.jsx
import React, { useState, useRef, useEffect } from 'react';
import { useParams, Link } from 'react-router-dom';
import { motion } from 'framer-motion';
import { ArrowLeft, Search, X, Flame, MapPin } from 'lucide-react';
import './styles/RestaurantPage.css';

const restaurants = {
    1: { id: 1, name: "Pizza Palace", banner: "https://images.unsplash.com/photo-1604382354936-07adb7bb5b25?w=1600" },
    2: { id: 2, name: "Burger Hub", banner: "https://images.unsplash.com/photo-1553979459-d2229ba7433b?w=1600" },
    3: { id: 3, name: "Sushi Master", banner: "https://images.unsplash.com/photo-1579751626657-72bc1701048f?w=1600" },
};

const menuData = {
    1: [
        { id: 1, name: "Маргарита Піца", price: 249, rating: 4.8, reviews: 324, popular: true, image: "https://images.unsplash.com/photo-1565299624946-b28f40a0ae38?w=1200", desc: "Класична італійська з базиліком", category: "Піца" },
        { id: 3, name: "Пепероні", price: 289, rating: 4.9, reviews: 412, popular: true, image: "https://images.unsplash.com/photo-1628840042765-0a9e5c3c8d5f?w=1200", desc: "Гостра салямі та сир", category: "Піца" },
        { id: 7, name: "Кока-Кола 0.5л", price: 45, rating: 4.6, reviews: 89, image: "https://images.unsplash.com/photo-1624555130581-1d9cca783bc0?w=800", desc: "Холодна", category: "Напої" },
    ],
    2: [
        { id: 2, name: "Бургер з яловичиною", price: 189, rating: 4.9, reviews: 287, popular: true, image: "https://images.unsplash.com/photo-1568901346375-23c9450c58cd?w=1200", desc: "180г яловичини, чеддер", category: "Бургери" },
        { id: 4, name: "Бекон Бургер", price: 229, rating: 4.8, reviews: 198, popular: true, image: "https://images.unsplash.com/photo-1553979459-d2229ba7433b?w=1200", desc: "Бекон + подвійний сир", category: "Бургери" },
        { id: 5, name: "Картопля фрі", price: 89, rating: 4.7, reviews: 156, image: "https://images.unsplash.com/photo-1576107232684-648eb609588b?w=800", desc: "Велика порція", category: "Додатки" },
    ],
    3: [
        { id: 6, name: "Філадельфія Класик", price: 329, rating: 4.9, reviews: 523, popular: true, image: "https://images.unsplash.com/photo-1579584425555-c3ce17fd4351?w=1200", desc: "Лосось, авокадо, сир", category: "Роли" },
    ],
};

const RestaurantPage = () => {
    const { id } = useParams();
    const restaurant = restaurants[id];
    const menu = menuData[id] || [];

    const [searchQuery, setSearchQuery] = useState('');
    const [selectedCategory, setSelectedCategory] = useState('all');
    const [sortBy, setSortBy] = useState('popular');
    const [userCity, setUserCity] = useState('Київ');
    const [userAddress, setUserAddress] = useState('Хрещатик, 22');

    const categoryRefs = useRef({});

    // Автовизначення міста по IP
    useEffect(() => {
        fetch('https://ipapi.co/json/')
            .then(res => res.json())
            .then(data => {
                if (data.city) {
                    setUserCity(data.city);
                    const addresses = {
                        'Київ': 'Хрещатик, 22',
                        'Львів': 'просп. Свободи, 7',
                        'Одеса': 'Дерибасівська, 10',
                        'Харків': 'вул. Сумська, 35',
                        'Дніпро': 'просп. Дмитра Яворницького, 50',
                    };
                    setUserAddress(addresses[data.city] || 'центр міста');
                }
            })
            .catch(() => {
                setUserCity('Київ');
                setUserAddress('Хрещатик, 22');
            });
    }, []);

    if (!restaurant) return <div className="not-found">Ресторан не знайдено</div>;

    // Фільтрація + сортування
    let displayedMenu = [...menu];
    if (searchQuery) {
        displayedMenu = displayedMenu.filter(dish =>
            dish.name.toLowerCase().includes(searchQuery.toLowerCase())
        );
    }
    if (selectedCategory !== 'all') {
        displayedMenu = displayedMenu.filter(dish => dish.category === selectedCategory);
    }
    displayedMenu.sort((a, b) => {
        if (sortBy === 'popular') return (b.popular ? 1 : 0) - (a.popular ? 1 : 0) || b.reviews - a.reviews;
        if (sortBy === 'price-asc') return a.price - b.price;
        if (sortBy === 'price-desc') return b.price - a.price;
        if (sortBy === 'rating') return b.rating - a.rating;
        return 0;
    });

    const categories = ['all', ...new Set(menu.map(d => d.category))];

    const scrollToCategory = (cat) => {
        setSelectedCategory(cat);
        if (cat === 'all') {
            window.scrollTo({ top: 0, behavior: 'smooth' });
        } else {
            categoryRefs.current[cat]?.scrollIntoView({ behavior: 'smooth', block: 'start' });
        }
    };

    return (
        <div className="restaurant-page-glovo">
            {/* БАНЕР + ЛОКАЦІЯ */}
            <div className="restaurant-hero">
                <img src={restaurant.banner} alt={restaurant.name} className="hero-bg" />
                <div className="hero-content">
                    <Link to="/restaurants" className="back-btn">
                        <ArrowLeft size={28} />
                    </Link>
                    <div>
                        <h1 className="restaurant-name">{restaurant.name}</h1>
                        <div className="restaurant-location">
                            <MapPin size={18} />
                            <span>Доставка з вул. {userAddress} — {userCity}</span>
                        </div>
                    </div>
                </div>
            </div>

            {/* ФІКСОВАНИЙ ХЕДЕР */}
            <div className="sticky-header">
                <div className="search-input-wrapper">
                    <Search size={22} className="search-icon" />
                    <input
                        type="text"
                        placeholder={`Пошук в ${restaurant.name}...`}
                        value={searchQuery}
                        onChange={(e) => setSearchQuery(e.target.value)}
                    />
                    {searchQuery && (
                        <X size={22} className="clear-search" onClick={() => setSearchQuery('')} />
                    )}
                </div>

                <div className="categories-scroll">
                    {categories.map(cat => (
                        <button
                            key={cat}
                            onClick={() => scrollToCategory(cat)}
                            className={`category-btn ${selectedCategory === cat ? 'active' : ''}`}
                        >
                            {cat === 'all' ? 'Усе меню' : cat}
                        </button>
                    ))}
                </div>

                <div className="sort-bar">
                    <select value={sortBy} onChange={(e) => setSortBy(e.target.value)}>
                        <option value="popular">Популярні</option>
                        <option value="price-asc">Спочатку дешевші</option>
                        <option value="price-desc">Спочатку дорожчі</option>
                        <option value="rating">За рейтингом</option>
                    </select>
                </div>
            </div>

            {/* МЕНЮ */}
            <div className="menu-content">
                {searchQuery ? (
                    <div className="search-results">
                        {displayedMenu.length === 0 ? (
                            <p className="no-results">Нічого не знайдено</p>
                        ) : (
                            displayedMenu.map(dish => (
                                <Link key={dish.id} to={`/dish/${dish.id}`} className="dish-card-search">
                                    <img src={dish.image} alt={dish.name} />
                                    <div className="dish-info">
                                        <h3>{dish.name}</h3>
                                        {dish.popular && <span className="hit"><Flame size={16} /> ХІТ</span>}
                                        <p>{dish.desc}</p>
                                        <div className="bottom">
                                            <span className="price">{dish.price} ₴</span>
                                            <span className="rating">★ {dish.rating} ({dish.reviews})</span>
                                        </div>
                                    </div>
                                </Link>
                            ))
                        )}
                    </div>
                ) : (
                    categories.filter(c => c !== 'all').map(category => {
                        const items = menu.filter(d => d.category === category);
                        if (items.length === 0) return null;

                        return (
                            <div key={category} ref={el => categoryRefs.current[category] = el} className="category-section">
                                <h2 className="category-title">
                                    {category}
                                    <span className="count">{items.length}</span>
                                </h2>
                                <div className="dishes-grid">
                                    {items.map((dish, i) => (
                                        <motion.div
                                            key={dish.id}
                                            initial={{ opacity: 0, y: 30 }}
                                            whileInView={{ opacity: 1, y: 0 }}
                                            viewport={{ once: true }}
                                            transition={{ delay: i * 0.05 }}
                                            whileHover={{ y: -12, scale: 1.02 }}
                                        >
                                            <Link to={`/dish/${dish.id}`} className="dish-card big">
                                                {dish.popular && <div className="hit-badge"><Flame size={18} /> ХІТ</div>}
                                                <img src={dish.image} alt={dish.name} />
                                                <div className="dish-info">
                                                    <h3>{dish.name}</h3>
                                                    <p className="desc">{dish.desc}</p>
                                                    <div className="bottom">
                                                        <span className="price">{dish.price} ₴</span>
                                                        <span className="rating">★ {dish.rating} ({dish.reviews})</span>
                                                    </div>
                                                </div>
                                            </Link>
                                        </motion.div>
                                    ))}
                                </div>
                            </div>
                        );
                    })
                )}
            </div>
        </div>
    );
};

export default RestaurantPage;