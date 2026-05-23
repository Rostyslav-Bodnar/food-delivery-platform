import { useEffect, useState } from "react";

import {
    createDish,
    updateDish,
    deleteDish
} from "../../../../api/Dish.ts";
import { useToast } from "../../../../global-components/toast/ToastContext";

const useDishMutations = (dishes) => {
    const [localDishes, setLocalDishes] = useState(dishes);
    const toast = useToast();

    useEffect(() => {
        setLocalDishes(dishes);
    }, [dishes]);

    const handleCreate = async (newDish) => {
        try {
            const created = await createDish(newDish);

            setLocalDishes(prev => [created, ...prev]);
            toast.success("Dish added to your menu");
        } catch (err) {
            console.error(err);
            if (!err?.toastShown) {
                toast.error(err.message ?? "Failed to add dish");
            }
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
            toast.success("Dish updated");
        } catch (err) {
            console.error(err);
            if (!err?.toastShown) {
                toast.error(err.message ?? "Failed to update dish");
            }
        }
    };

    const handleDelete = async (id) => {
        try {
            await deleteDish(id);

            setLocalDishes(prev =>
                prev.filter(d => d.id !== id)
            );
            toast.success("Dish removed");
        } catch (err) {
            console.error(err);
            if (!err?.toastShown) {
                toast.error(err.message ?? "Failed to remove dish");
            }
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
