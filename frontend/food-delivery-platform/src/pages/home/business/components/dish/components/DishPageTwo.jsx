import { IngredientsList } from "./IngredientsList";

export function DishPageTwo({
                                form,
                                change,
                                setPage,
                                submit,
                                editing,
                                ingredientHandlers
                            }) {
    return (
        <>
            <div className="modal-row two">
                <div>
                    <label>Price, ₴</label>
                    <input
                        type="number"
                        value={form.price}
                        onChange={e => change("price", e.target.value)}
                    />
                </div>

                <div>
                    <label>Cooking Time (min)</label>
                    <input
                        type="number"
                        value={form.cookingTime}
                        onChange={e => change("cookingTime", e.target.value)}
                    />
                </div>
            </div>

            <IngredientsList
                ingredients={form.ingredients}
                {...ingredientHandlers}
            />

            <div className="modal-row row-actions">
                <button className="btn ghost" onClick={() => setPage(1)}>
                    ← Back
                </button>
                <button className="btn primary" onClick={submit}>
                    {editing ? "Save" : "Add"}
                </button>
            </div>
        </>
    );
}