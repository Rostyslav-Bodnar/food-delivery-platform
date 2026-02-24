import React from 'react';
import { motion } from 'framer-motion';
import { Search, Filter } from 'lucide-react';

const SearchHero = ({ searchTerm, setSearchTerm, filteredCount, onFilterClick }) => {
    return (
        <motion.section
            className="search-hero"
            initial={{ opacity: 0, y: 20 }}
            animate={{ opacity: 1, y: 0 }}
            transition={{ delay: 0.2 }}
        >
            <div className="search-container">
                <Search size={24} className="search-icon" />
                <input
                    type="text"
                    placeholder="What would you like to eat?.."
                    value={searchTerm}
                    onChange={(e) => setSearchTerm(e.target.value)}
                    className="search-input"
                />
                {searchTerm && (
                    <motion.div initial={{ opacity: 0 }} animate={{ opacity: 1 }} className="search-suggestions">
                        <p>Search: <strong>"{searchTerm}"</strong> — found {filteredCount} dishes</p>
                    </motion.div>
                )}
            </div>
            <motion.button
                whileHover={{ scale: 1.05 }}
                whileTap={{ scale: 0.95 }}
                onClick={onFilterClick}
                className="filter-toggle"
            >
                <Filter size={20} /> Filters
            </motion.button>
        </motion.section>
    );
};

export default SearchHero;