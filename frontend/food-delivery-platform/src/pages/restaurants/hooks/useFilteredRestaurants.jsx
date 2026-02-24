// src/hooks/useFilteredRestaurants.js
import {useMemo} from 'react';

const useFilteredRestaurants = (restaurants, searchQuery, selectedCategory, sortBy) => {
    return useMemo(() => {
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
};

export default useFilteredRestaurants;