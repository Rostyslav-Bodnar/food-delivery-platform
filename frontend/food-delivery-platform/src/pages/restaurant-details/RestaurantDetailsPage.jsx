// Assuming this is src/pages/RestaurantDetailsPage.js
import React from 'react';
import { Link, useLocation } from 'react-router-dom';
import { motion } from 'framer-motion';
import { ArrowLeft, Flame, MapPin } from 'lucide-react';
import './styles/RestaurantDetailsPage.css';
import DishCardComponent from "../DishCardComponent.jsx";
import StickyHeader from "./components/StickyHeader.jsx";
import useRestaurantMenu from "./hooks/useRestaurantMenu";
import useUserLocation from "./hooks/useUserLocation";
import useMenuDisplay from "./hooks/useMenuDisplay";

const RestaurantDetailsPage = () => {
    const { state } = useLocation();
    const restaurant = state?.restaurant;

    if (!restaurant) {
        return <div className="not-found">Ресторан не знайдено</div>;
    }
    console.log(state);

    const { menu, loading } = useRestaurantMenu(restaurant.id);
    const { userCity, userAddress } = useUserLocation();
    const {
        searchQuery,
        setSearchQuery,
        selectedCategory,
        setSelectedCategory,
        sortBy,
        setSortBy,
        displayedMenu,
        categories,
        categoryRefs,
        scrollToCategory
    } = useMenuDisplay(menu);

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
                            <span>Delivery from {userAddress} — {userCity}</span>
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
                            <p className="no-results">Nothing found</p>
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