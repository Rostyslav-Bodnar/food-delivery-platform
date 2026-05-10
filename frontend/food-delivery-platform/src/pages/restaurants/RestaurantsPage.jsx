import React, { useEffect } from "react";
import { motion, AnimatePresence } from "framer-motion";
import { Store } from "lucide-react";
import "./styles/RestaurantsPage.css";

import CustomerSidebar from "../sidebars/CustomerSidebar.jsx";
import RestaurantCard from "./components/RestaurantCard.jsx";
import RestaurantsFilter from "./components/RestaurantsFilter.jsx";

import useFetchRestaurants from "./hooks/useFetchRestaurants";
import useRestaurantFilters from "./hooks/useRestaurantFilters";
import useFilteredRestaurants from "./hooks/useFilteredRestaurants";

import { useToast } from "../../global-components/toast/ToastContext.jsx";

const RestaurantsPage = () => {
    const toast = useToast();

    const {
        restaurants,
        loading,
        error,
        retry,
        clearError
    } = useFetchRestaurants();

    const {
        searchQuery,
        setSearchQuery,
        selectedCategory,
        setSelectedCategory,
        sortBy,
        setSortBy
    } = useRestaurantFilters();

    const filteredAndSorted = useFilteredRestaurants(
        restaurants,
        searchQuery,
        selectedCategory,
        sortBy
    );

    // =========================
    // ERROR → TOAST
    // =========================
    useEffect(() => {
        if (!error) return;

        toast.addToast({
            message: error
        });

        clearError();
    }, [error, toast]);

    return (
        <div className="app-wrapper">
            <CustomerSidebar />

            <div className="main-content">
                {/* particles */}
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
                    {/* Header */}
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

                    {/* Loading */}
                    {loading && (
                        <div style={{ color: "var(--muted)" }}>
                            Loading establishments...
                        </div>
                    )}

                    {/* Content */}
                    {!loading && (
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
                                    {filteredAndSorted.map(
                                        restaurant => (
                                            <RestaurantCard
                                                key={restaurant.id}
                                                restaurant={restaurant}
                                            />
                                        )
                                    )}
                                </motion.div>
                            )}
                        </AnimatePresence>
                    )}
                </div>
            </div>
        </div>
    );
};

export default RestaurantsPage;