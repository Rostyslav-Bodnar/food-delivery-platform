// src/hooks/useRestaurantMenu.js
import { useState, useEffect } from 'react';
import { getDishesForCustomerByBusinessId } from "../../../api/Dish.jsx";
import { CategoryMap } from "../../../constants/category.jsx";

const useRestaurantMenu = (restaurantId) => {
    const [menu, setMenu] = useState([]);
    const [loading, setLoading] = useState(true);

    useEffect(() => {
        const loadMenu = async () => {
            try {
                const dishes = await getDishesForCustomerByBusinessId(restaurantId);
                setMenu(
                    dishes.map(d => ({
                        ...d,
                        image: d.imageUrl,
                        desc: d.description,
                        rating: d.rating ?? 5, // або з бекенду
                        reviews: d.reviews ?? 0, // або з бекенду
                        popular: d.popular ?? false, // або з бекенду
                        restaurant: d.restaurantName || '', // assuming restaurant.name is not directly available, adjust if needed
                        category: CategoryMap[d.category]
                    }))
                );
            } catch (e) {
                console.error("Menu loading error:", e);
            } finally {
                setLoading(false);
            }
        };
        loadMenu();
    }, [restaurantId]);

    return { menu, loading };
};

export default useRestaurantMenu;