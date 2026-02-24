import React, { useState, useEffect } from 'react';
import { motion } from 'framer-motion';
import CustomerSidebar from '../../sidebars/CustomerSidebar';
import { getAllDishesForCustomer } from '../../../api/Dish.jsx';
import { CategoryMap } from '../../../constants/category.jsx';

import SearchHero from './components/SearchHero';
import FilterSidebar from './components/FilterSidebar';
import PopularSection from './components/PopularSection';
import AllDishesSection from './components/AllDishesSection';

import "../styles/CustomerHomePage.css";

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
                // Mock top dishes
                const mockDishes = [
                    { id: 1, name: "Margherita Pizza", image: "...", rating: 4.8, restaurant: "Pizza Palace", price: 249, category: "pizza", popular: true },
                    { id: 2, name: "Beef Burger", image: "...", rating: 4.9, restaurant: "Burger Hub", price: 189, category: "burger", popular: true },
                    { id: 3, name: "Dragon Sushi Set", image: "...", rating: 4.7, restaurant: "Sushi Master", price: 429, category: "sushi", popular: true },
                ];
                setPopularDishes(mockDishes.filter(d => d.popular).sort((a, b) => b.rating - a.rating));

                setLoading(true);
                const data = await getAllDishesForCustomer();
                const mappedDishes = data.map(d => ({
                    id: d.id,
                    name: d.name,
                    image: d.imageUrl,
                    rating: 4.5, // default rating
                    restaurant: d.businessDetails.name,
                    price: d.price,
                    category: CategoryMap[d.category]
                }));
                setAllDishes(mappedDishes);
                setFilteredDishes(mappedDishes);
            } catch (err) {
                console.error("Error loading dishes:", err);
            } finally {
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
            <CustomerSidebar />

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

                <SearchHero
                    searchTerm={searchTerm}
                    setSearchTerm={setSearchTerm}
                    filteredCount={filteredDishes.length}
                    onFilterClick={() => setIsFilterOpen(true)}
                />

                <FilterSidebar
                    isOpen={isFilterOpen}
                    onClose={() => setIsFilterOpen(false)}
                    selectedCategory={selectedCategory}
                    setSelectedCategory={setSelectedCategory}
                    selectedRating={selectedRating}
                    setSelectedRating={setSelectedRating}
                    priceRange={priceRange}
                    setPriceRange={setPriceRange}
                    filteredCount={filteredDishes.length}
                />

                <PopularSection popularDishes={popularDishes} scrollPopular={scrollPopular} />

                <AllDishesSection filteredDishes={filteredDishes} loading={loading} />
            </div>
        </div>
    );
};

export default CustomerHomePage;