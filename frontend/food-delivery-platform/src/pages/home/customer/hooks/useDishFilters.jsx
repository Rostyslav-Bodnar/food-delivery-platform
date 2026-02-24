import { useEffect, useState } from "react";

export const useDishFilters = (allDishes) => {
    const [filteredDishes, setFilteredDishes] = useState([]);
    const [searchTerm, setSearchTerm] = useState("");
    const [selectedCategory, setSelectedCategory] = useState("all");
    const [selectedRating, setSelectedRating] = useState("all");
    const [priceRange, setPriceRange] = useState([0, 5000]);

    useEffect(() => {
        let filtered = allDishes;

        if (searchTerm) {
            filtered = filtered.filter(d =>
                d.name.toLowerCase().includes(searchTerm.toLowerCase()) ||
                d.restaurant.toLowerCase().includes(searchTerm.toLowerCase())
            );
        }

        if (selectedCategory !== "all") {
            filtered = filtered.filter(d => d.category === selectedCategory);
        }

        if (selectedRating !== "all") {
            filtered = filtered.filter(d => d.rating >= parseFloat(selectedRating));
        }

        filtered = filtered.filter(d =>
            d.price >= priceRange[0] && d.price <= priceRange[1]
        );

        setFilteredDishes(filtered);
    }, [searchTerm, selectedCategory, selectedRating, priceRange, allDishes]);

    return {
        filteredDishes,
        searchTerm,
        setSearchTerm,
        selectedCategory,
        setSelectedCategory,
        selectedRating,
        setSelectedRating,
        priceRange,
        setPriceRange
    };
};