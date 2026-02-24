export function IngredientsList({
                                    ingredients,
                                    updateIngredient,
                                    removeIngredient,
                                    addIngredient
                                }) {
    return (
        <>
            <h4>Ingredients</h4>

            {ingredients.map((ing, i) => (
                <div key={i} className="ingredient-row">
                    <input
                        placeholder="Name"
                        value={ing.name}
                        onChange={e =>
                            updateIngredient(i, "name", e.target.value)
                        }
                    />
                    <input
                        type="number"
                        placeholder="Weight, g"
                        value={ing.weight}
                        onChange={e =>
                            updateIngredient(i, "weight", Number(e.target.value))
                        }
                    />
                    <button
                        onClick={() => removeIngredient(i)}
                        className="btn danger small"
                    >
                        X
                    </button>
                </div>
            ))}

            <button onClick={addIngredient} className="btn ghost">
                + Add Ingredient
            </button>
        </>
    );
}