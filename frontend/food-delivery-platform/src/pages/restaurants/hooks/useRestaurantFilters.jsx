// src/hooks/useRestaurantFilters.js
import { useState } from 'react';

const useRestaurantFilters = () => {
    const [searchQuery, setSearchQuery] = useState('');
    const [selectedCategory, setSelectedCategory] = useState('all');
    const [sortBy, setSortBy] = useState('rating');

    return {
        searchQuery,
        setSearchQuery,
        selectedCategory,
        setSelectedCategory,
        sortBy,
        setSortBy
    };
};

export default useRestaurantFilters;