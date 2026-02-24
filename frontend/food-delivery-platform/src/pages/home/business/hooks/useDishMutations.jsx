// src/hooks/useDishMutations.js
import { useState } from "react";
import { createDish, updateDish, deleteDish } from "../../../../api/Dish.jsx";

const useDishMutations = (dishes) => {
    const [localDishes, setLocalDishes] = useState(dishes);

    const handleCreate = async (newDish) => {
        try {
            const created = await createDish(newDish);
            setLocalDishes(prev => [created, ...prev]);
        } catch (err) {
            console.log(newDish);
            console.error(err);
            alert("Помилка створення страви");
        }
    };

    const handleUpdate = async (id, patch) => {
        try {
            const updated = await updateDish(id, patch);
            setLocalDishes(prev =>
                prev.map(d => (d.id === id ? updated : d))
            );
        } catch (err) {
            console.error(err);
            alert("Помилка оновлення страви");
        }
    };

    const handleDelete = async (id) => {
        try {
            await deleteDish(id);
            setLocalDishes(prev => prev.filter(d => d.id !== id));
        } catch (err) {
            console.error(err);
            alert("Не вдалося видалити страву");
        }
    };

    return { handleCreate, handleUpdate, handleDelete };
};

export default useDishMutations;