import { useEffect, useState } from "react";

export function useDishForm(editing, open) {
    const [page, setPage] = useState(1);

    const initialState = {
        name: "",
        category: "pizza",
        price: "",
        description: "",
        cookingTime: 10,
        ingredients: [],
        imageFile: null,
        imagePreview: ""
    };

    const [form, setForm] = useState(initialState);

    useEffect(() => {
        if (editing) {
            setForm({
                name: editing.name || "",
                category: editing.category || "pizza",
                price: editing.price ?? "",
                description: editing.description || "",
                cookingTime: editing.cookingTime || 10,
                ingredients: editing.ingredients || [],
                imageFile: null,
                imagePreview: editing.imageUrl || editing.image || ""
            });
        } else {
            setForm(initialState);
        }

        setPage(1);
    }, [editing, open]);

    const change = (key, value) =>
        setForm(prev => ({ ...prev, [key]: value }));

    const updateIngredient = (index, key, value) => {
        const updated = [...form.ingredients];
        updated[index][key] = value;
        change("ingredients", updated);
    };

    const addIngredient = () =>
        change("ingredients", [...form.ingredients, { name: "", weight: 0 }]);

    const removeIngredient = (index) =>
        change("ingredients", form.ingredients.filter((_, i) => i !== index));

    return {
        form,
        setForm,
        page,
        setPage,
        change,
        updateIngredient,
        addIngredient,
        removeIngredient
    };
}