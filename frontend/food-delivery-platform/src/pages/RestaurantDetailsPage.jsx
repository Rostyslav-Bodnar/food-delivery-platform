import React, { useState, useRef, useEffect } from 'react';
import { Link, useLocation } from 'react-router-dom';
import { motion } from 'framer-motion';
import { ArrowLeft, Flame, MapPin } from 'lucide-react';
import './styles/RestaurantDetailsPage.css';
import DishCardComponent from "../components/customer-components/DishCardComponent.jsx";
import {getDishesForCustomerByBusinessId} from "../api/Dish.jsx";
import {CategoryMap} from "../constants/category.jsx";
import StickyHeader from "../components/restaurant/StickyHeader.jsx";

const RestaurantDetailsPage = () => {
    const { state } = useLocation();
    const [menu, setMenu] = useState([]);
    const [loading, setLoading] = useState(true);
    
    const restaurant = state?.restaurant;

    // ❗ Якщо зайшли напряму (refresh) → показуємо 404
    if (!restaurant) {
        return <div className="not-found">Ресторан не знайдено</div>;
    }

    console.log(state);
    useEffect(() => {
        const loadMenu = async () => {
            try {
                const dishes = await getDishesForCustomerByBusinessId(restaurant.id);
                setMenu(
                    dishes.map(d => ({
                        ...d,
                        image: d.imageUrl,
                        desc: d.description,
                        rating: d.rating ?? 5,       // або з бекенду
                        reviews: d.reviews ?? 0,     // або з бекенду
                        popular: d.popular ?? false, // або з бекенду
                        restaurant: restaurant.name,
                        category: CategoryMap[d.category]
                    }))
                );
            } catch (e) {
                console.error("Помилка завантаження меню:", e);
            } finally {
                setLoading(false);
            }
        };

        loadMenu();
    }, [restaurant.id]);

    // UI state
    const [searchQuery, setSearchQuery] = useState('');
    const [selectedCategory, setSelectedCategory] = useState('all');
    const [sortBy, setSortBy] = useState('popular');
    const [userCity, setUserCity] = useState('Київ');
    const [userAddress, setUserAddress] = useState('Хрещатик, 22');

    const categoryRefs = useRef({});

    // Автовизначення міста
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

    // Фільтрація меню
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

            {/* БАНЕР */}
            <div className="restaurant-hero">
                <img
                    src={restaurant.image || "https://via.placeholder.com/1600x500?text=Restaurant"}
                    alt={restaurant.name}
                    className="hero-bg"
                />

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

            <StickyHeader
                restaurantName={restaurant.name}
                searchQuery={searchQuery}
                onSearchChange={setSearchQuery}
                onClearSearch={() => setSearchQuery("")}
                sortBy={sortBy}
                onSortChange={setSortBy}
                categories={categories}
                selectedCategory={selectedCategory}
                onCategorySelect={scrollToCategory}
            />

            {/* МЕНЮ */}
            <div className="menu-content">
                {searchQuery ? (
                    <div className="search-results">
                        {displayedMenu.length === 0 ? (
                            <p className="no-results">Нічого не знайдено</p>
                        ) : (
                            displayedMenu.map(dish => (
                                <DishCardComponent key={dish.id} dish={dish} isPopular={dish.popular} />
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
                                            whileHover={{ y: -5, scale: 1.0 }}
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

export default RestaurantDetailsPage;
