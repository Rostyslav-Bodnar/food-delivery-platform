import React from "react";
import { motion, AnimatePresence } from "framer-motion";
import { X } from "lucide-react";
import { CategoryList } from "../../../../constants/category.jsx";

const FilterSidebar = ({
                           isOpen,
                           onClose,
                           selectedCategory,
                           setSelectedCategory,
                           selectedRating,
                           setSelectedRating,
                           priceRange,
                           setPriceRange,
                           filteredCount
                       }) => {
    return (
        <AnimatePresence>
            {isOpen && (
                <>
                    <motion.aside className="filter-sidebar">
                        <div className="filter-header">
                            <h3>Filters</h3>
                            <button onClick={onClose}>
                                <X size={26} />
                            </button>
                        </div>

                        <div className="filter-group">
                            <label>Category</label>
                            <select
                                value={selectedCategory}
                                onChange={(e) => setSelectedCategory(e.target.value)}
                            >
                                <option value="all">All dishes</option>
                                {CategoryList.map(cat => (
                                    <option key={cat.id} value={cat.name}>{cat.name}</option>
                                ))}
                            </select>
                        </div>

                        <div className="filter-group">
                            <label>Minimum rating</label>
                            <select
                                value={selectedRating}
                                onChange={(e) => setSelectedRating(e.target.value)}
                            >
                                <option value="all">No limit</option>
                                <option value="4.5">4.5+</option>
                                <option value="4.0">4.0+</option>
                                <option value="3.5">3.5+</option>
                            </select>
                        </div>

                        <div className="filter-group">
                            <label>Price: {priceRange[0]}₴ — {priceRange[1]}₴</label>
                            <input
                                type="range"
                                min="0"
                                max="5000"
                                value={priceRange[1]}
                                onChange={(e) => setPriceRange([priceRange[0], parseInt(e.target.value)])}
                            />
                        </div>

                        <button onClick={onClose} className="apply-filters-btn">
                            Show {filteredCount} dishes
                        </button>
                    </motion.aside>

                    <motion.div className="filter-overlay" onClick={onClose} />
                </>
            )}
        </AnimatePresence>
    );
};

export default FilterSidebar;