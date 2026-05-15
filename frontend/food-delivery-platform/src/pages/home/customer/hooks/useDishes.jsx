import { useEffect, useState } from "react";
import { getAllDishesForCustomer } from "../../../../api/Dish.ts";
import { CategoryMap } from "../../../../constants/category";

export const useDishes = () => {
    const [popularDishes, setPopularDishes] = useState([]);
    const [allDishes, setAllDishes] = useState([]);
    const [loading, setLoading] = useState(true);

    useEffect(() => {
        const fetchDishes = async () => {
            try {
                const mockDishes = [
                    { id: 1, name: "Margherita Pizza", image: "...", rating: 4.8, restaurant: "Pizza Palace", price: 249, category: "pizza", popular: true },
                    { id: 2, name: "Beef Burger", image: "...", rating: 4.9, restaurant: "Burger Hub", price: 189, category: "burger", popular: true },
                    { id: 3, name: "Dragon Sushi Set", image: "...", rating: 4.7, restaurant: "Sushi Master", price: 429, category: "sushi", popular: true },
                ];

                const topDishes = mockDishes
                    .filter(d => d.popular)
                    .sort((a, b) => b.rating - a.rating);

                setPopularDishes(topDishes);

                setLoading(true);

                const data = await getAllDishesForCustomer();

                const mappedDishes = data.map(d => ({
                    id: d.id,
                    name: d.name,
                    image: d.imageUrl,
                    rating: 4.5,
                    restaurant: d.businessDetails.name,
                    price: d.price,
                    category: CategoryMap[d.category]
                }));

                setAllDishes(mappedDishes);
            }
            catch (err) {
                console.error("Error loading dishes:", err);
            }
            finally {
                setLoading(false);
            }
        };

        fetchDishes();
    }, []);

    return { popularDishes, allDishes, loading };
};