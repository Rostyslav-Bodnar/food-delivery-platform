// src/hooks/useFilteredDishes.js
import { useMemo } from "react";

const useFilteredDishes = (dishes, q, category, onlyPopular, sortBy) => {
    const filtered = useMemo(() => {
        let out = dishes.slice();
        if (q.trim()) out = out.filter(d => d.name.toLowerCase().includes(q.toLowerCase()));
        if (category !== "all")
            out = out.filter(d => d.category === category);
        if (onlyPopular) out = out.filter(d => d.popular);
        if (sortBy === "name") out.sort((a, b) => a.name.localeCompare(b.name));
        if (sortBy === "price") out.sort((a, b) => a.price - b.price);
        if (sortBy === "rating") out.sort((a, b) => b.rating - a.rating);
        return out;
    }, [dishes, q, category, onlyPopular, sortBy]);

    return { filtered };
};

export default useFilteredDishes;