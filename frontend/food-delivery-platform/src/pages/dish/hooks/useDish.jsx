import { useEffect, useState } from "react";
import { getDishForCustomer } from "../../../api/Dish.ts";
import { mapDishToViewModel } from "../services/dishMapper";

const mockReviews = {
    1: [
        { id: 1, author: "Олена", rating: 5, text: "Найкраща Маргарита!", date: "2 дні тому" },
        { id: 2, author: "Макс", rating: 4, text: "Хотілося б більше базиліку.", date: "1 тиждень тому" },
    ],
    2: [
        { id: 1, author: "Ігор", rating: 5, text: "Соковитий і великий бургер.", date: "3 дні тому" }
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
                console.error("Помилка завантаження страви:", err);
            } finally {
                setLoading(false);
            }
        };

        loadDish();
    }, [id]);

    return { dish, loading };
};
