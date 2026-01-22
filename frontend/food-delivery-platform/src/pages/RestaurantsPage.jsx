import React, { useState, useMemo, useEffect } from 'react';
import { motion, AnimatePresence } from 'framer-motion';
import { Store } from 'lucide-react';
import './styles/RestaurantsPage.css';
import CustomerSidebar from "../components/customer-components/CustomerSidebar.jsx";
import { getAllBusinessAccounts } from "../api/Account.jsx";
import RestaurantCard from "../components/restaurantsPage/RestaurantCard.jsx";
import RestaurantsFilter from "../components/restaurantsPage/RestaurantsFilter.jsx";

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
                        deliveryPrice: "Free",
                        category: r.description ?? "Restaurant",
                    }))
                );
            } catch (e) {
                console.error("Error happened while getting business account:", e);
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
                            <Store size={40} /> All establishments
                        </h1>

                        <RestaurantsFilter
                            searchQuery={searchQuery}
                            onSearchChange={setSearchQuery}
                            onClearSearch={() => setSearchQuery("")}
                            selectedCategory={selectedCategory}
                            onCategoryChange={setSelectedCategory}
                            sortBy={sortBy}
                            onSortChange={setSortBy}
                        />

                    </motion.div>

                    <AnimatePresence mode="wait">
                        {filteredAndSorted.length === 0 ? (
                            <motion.div
                                key="no-results"
                                initial={{ opacity: 0 }}
                                animate={{ opacity: 1 }}
                                className="no-results"
                            >
                                <p>Nothing was found</p>
                            </motion.div>
                        ) : (
                            <motion.div className="restaurants-grid">
                                {filteredAndSorted.map((restaurant) => (
                                    <RestaurantCard
                                        key={restaurant.id}
                                        restaurant={restaurant}
                                    />
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
