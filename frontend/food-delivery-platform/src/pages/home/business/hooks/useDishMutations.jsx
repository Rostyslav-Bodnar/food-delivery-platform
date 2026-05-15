import { useEffect, useState } from "react";

import {
    createDish,
    updateDish,
    deleteDish
} from "../../../../api/Dish.ts";

const useDishMutations = (dishes) => {
    const [localDishes, setLocalDishes] = useState(dishes);


    useEffect(() => {
        setLocalDishes(dishes);
    }, [dishes]);

    const handleCreate = async (newDish) => {
        try {
            const created = await createDish(newDish);

            setLocalDishes(prev => [created, ...prev]);
        } catch (err) {
            console.error(err);
        }
    };

    const handleUpdate = async (request) => {
        try {
            const updated = await updateDish(request);

            setLocalDishes(prev =>
                prev.map(d =>
                    d.id === updated.id
                        ? updated
                        : d
                )
            );
        } catch (err) {
            console.error(err);

            toast.addToast({
                message: err.message,
            });
        }
    };

    const handleDelete = async (id) => {
        try {
            await deleteDish(id);

            setLocalDishes(prev =>
                prev.filter(d => d.id !== id)
            );
        } catch (err) {
            console.error(err);

            toast.addToast({
                message: err.message,
            });
        }
    };

    return {
        localDishes,
        handleCreate,
        handleUpdate,
        handleDelete
    };
};

export default useDishMutations;