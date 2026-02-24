import { useDishForm } from "./hooks/useDishForm";
import { useImageUpload } from "./hooks/useImageUpload";
import { DishPageOne } from "./components/DishPageOne";
import { DishPageTwo } from "./components/DishPageTwo";

export default function DishComponent({
                                          open,
                                          onClose,
                                          onCreate,
                                          onUpdate,
                                          editing,
                                          userData
                                      }) {
    const {
        form,
        setForm,
        page,
        setPage,
        change,
        updateIngredient,
        addIngredient,
        removeIngredient
    } = useDishForm(editing, open);

    const imageUpload = useImageUpload(setForm);

    const submit = () => {
        const payload = {
            userId: userData.id,
            menuId: null,
            name: form.name,
            description: form.description,
            price: Number(form.price) || 0,
            category: form.category,
            cookingTime: Number(form.cookingTime),
            ingredients: form.ingredients,
        };

        if (form.imageFile) payload.image = form.imageFile;

        editing
            ? onUpdate(editing.id, payload)
            : onCreate(payload);

        onClose();
    };

    if (!open) return null;

    return (
        <div className="bh-modal" onClick={onClose}>
            <div className="bh-modal-card" onClick={e => e.stopPropagation()}>
                <h3>{editing ? "Edit" : "New Dish"}</h3>

                {page === 1 && (
                    <DishPageOne
                        form={form}
                        change={change}
                        setPage={setPage}
                        onClose={onClose}
                        imageUpload={imageUpload}
                    />
                )}

                {page === 2 && (
                    <DishPageTwo
                        form={form}
                        change={change}
                        setPage={setPage}
                        submit={submit}
                        editing={editing}
                        ingredientHandlers={{
                            updateIngredient,
                            addIngredient,
                            removeIngredient
                        }}
                    />
                )}
            </div>
        </div>
    );
}