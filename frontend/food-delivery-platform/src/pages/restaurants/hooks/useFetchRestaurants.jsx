// src/hooks/useFetchRestaurants.js
import { useState, useEffect } from 'react';
import { getAllBusinessAccounts } from "../../../api/Account.jsx";

const useFetchRestaurants = () => {
    const [restaurants, setRestaurants] = useState([]);

    useEffect(() => {
        const fetchRestaurants = async () => {
            try {
                const response = await getAllBusinessAccounts();
                setRestaurants(
                    response.map(r => ({
                        id: r.id,
                        name: r.name,
                        image: r.imageUrl,
                        description: r.description,
                        // бо бек поки не повертає рейтинг/доставку — ставимо заглушки
                        rating: 4.8,
                        deliveryTime: "25-40 хв",
                        deliveryPrice: "Free",
                        category: r.description ?? "Restaurant",
                    }))
                );
            } catch (e) {
                console.error("Error happened while getting business account:", e);
            }
        };
        fetchRestaurants();
    }, []);

    return { restaurants };
};

export default useFetchRestaurants;