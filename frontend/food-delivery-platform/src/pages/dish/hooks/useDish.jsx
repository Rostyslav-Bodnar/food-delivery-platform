import { useEffect, useState } from "react";
import { getDishForCustomer } from "../../../api/Dish.ts";
import { mapDishToViewModel } from "../services/dishMapper";

const mockReviews = {
    1: [
        { id: 1, author: "Olena", rating: 5, text: "Best Margherita!", date: "2 days ago" },
        { id: 2, author: "Max", rating: 4, text: "Would like more basil.", date: "1 week ago" },
    ],
    2: [
        { id: 1, author: "Ihor", rating: 5, text: "Juicy and huge burger.", date: "3 days ago" }
    ]
};

export const useDish = (id) => {
    const [dish, setDish] = useState(null);
    const [loading, setLoading] = useState(true);

    useEffect(() => {
        const loadDish = async () => {
            try {
                const data = await getDishForCustomer(id);
                const reviews = mockReviews[id] ?? [];
                const mapped = mapDishToViewModel(data, reviews);

                setDish(mapped);
            } catch (err) {
                console.error("Failed to load dish:", err);
            } finally {
                setLoading(false);
            }
        };

        loadDish();
    }, [id]);

    return { dish, loading };
};
