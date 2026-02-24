// src/hooks/useFetchDishes.js
import { useEffect, useState } from "react";
import { getDishesByBusinessId } from "../../../../api/Dish.jsx";

const useFetchDishes = (userData) => {
    const [dishes, setDishes] = useState([]);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState(null);
    const businessId = userData.currentAccount?.id;

    useEffect(() => {
        const loadDishes = async () => {
            try {
                setLoading(true);
                const res = await getDishesByBusinessId(businessId);
                setDishes(res);
            } catch (err) {
                console.error(err);
                setError("Не вдалося завантажити список страв.");
            } finally {
                setLoading(false);
            }
        };
        loadDishes();
    }, [businessId]);

    return { dishes, loading, error };
};

export default useFetchDishes;