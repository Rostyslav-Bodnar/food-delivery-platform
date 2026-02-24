// src/hooks/useMenuDisplay.js
import { useState, useRef, useMemo } from 'react';

const useMenuDisplay = (menu) => {
    const [searchQuery, setSearchQuery] = useState('');
    const [selectedCategory, setSelectedCategory] = useState('all');
    const [sortBy, setSortBy] = useState('popular');
    const categoryRefs = useRef({});

    const categories = useMemo(() => ['all', ...new Set(menu.map(d => d.category))], [menu]);

    const displayedMenu = useMemo(() => {
        let filtered = [...menu];
        if (searchQuery) {
            filtered = filtered.filter(dish =>
                dish.name.toLowerCase().includes(searchQuery.toLowerCase())
            );
        }
        if (selectedCategory !== 'all') {
            filtered = filtered.filter(dish => dish.category === selectedCategory);
        }
        filtered.sort((a, b) => {
            if (sortBy === 'popular') return (b.popular ? 1 : 0) - (a.popular ? 1 : 0) || b.reviews - a.reviews;
            if (sortBy === 'price-asc') return a.price - b.price;
            if (sortBy === 'price-desc') return b.price - a.price;
            if (sortBy === 'rating') return b.rating - a.rating;
            return 0;
        });
        return filtered;
    }, [menu, searchQuery, selectedCategory, sortBy]);

    const scrollToCategory = (cat) => {
        setSelectedCategory(cat);
        if (cat === 'all') {
            window.scrollTo({ top: 0, behavior: 'smooth' });
        } else {
            categoryRefs.current[cat]?.scrollIntoView({ behavior: 'smooth', block: 'start' });
        }
    };

    return {
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
    };
};

export default useMenuDisplay;