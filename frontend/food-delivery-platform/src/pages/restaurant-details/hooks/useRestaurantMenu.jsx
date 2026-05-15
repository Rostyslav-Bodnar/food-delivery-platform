// src/hooks/useRestaurantMenu.js

import { useState, useEffect } from "react";

import { getDishesForCustomerByBusinessId }
    from "../../../api/Dish.ts";

import { CategoryMap }
    from "../../../constants/category";

const useRestaurantMenu = (restaurantId) => {
    const [menu, setMenu] = useState([]);
    const [loading, setLoading] = useState(true);
    
    useEffect(() => {
        if (!restaurantId) return;

        const loadMenu = async () => {
            try {
                setLoading(true);

                const dishes =
                    await getDishesForCustomerByBusinessId(
                        restaurantId
                    );

                setMenu(
                    dishes.map(d => ({
                        ...d,
                        image: d.imageUrl,
                        desc: d.description,
                        rating: d.rating ?? 5,
                        reviews: d.reviews ?? 0,
                        popular: d.popular ?? false,
                        restaurant: d.restaurantName || "",
                        category: CategoryMap[d.category]
                    }))
                );
            } catch (e) {
                console.error(
                    "Menu loading error:",
                    e
                );
            } finally {
                setLoading(false);
            }
        };

        loadMenu();
    }, [restaurantId]);

    return {
        menu,
        loading
    };
};

export default useRestaurantMenu;