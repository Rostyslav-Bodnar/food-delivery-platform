import React, { useEffect, useRef, useState } from "react";
import {CategoryList} from "../constants/category.jsx";

export default function DishComponent({ open, onClose, onCreate, onUpdate, editing, userData }) {
    const [page, setPage] = useState(1); // 🔥 СТОРІНКА 1/2

    const [form, setForm] = useState({
        name: "",
        category: "pizza",
        price: "",
        description: "",
        cookingTime: 10,
        ingredients: [],
        imageFile: null,
        imagePreview: ""
    });

    const dropRef = useRef(null);
    const prevObjectUrlRef = useRef(null);

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
            setForm({
                name: "",
                category: "pizza",
                price: "",
                description: "",
                cookingTime: 10,
                ingredients: [],
                imageFile: null,
                imagePreview: ""
            });
        }

        setPage(1); // reset page on open

        return () => {
            if (prevObjectUrlRef.current) {
                URL.revokeObjectURL(prevObjectUrlRef.current);
                prevObjectUrlRef.current = null;
            }
        };
    }, [editing, open]);

    const change = (k, v) => setForm(prev => ({ ...prev, [k]: v }));

    const updateIngredient = (index, key, value) => {
        const updated = [...form.ingredients];
        updated[index][key] = value;
        change("ingredients", updated);
    };

    const addIngredient = () => {
        change("ingredients", [...form.ingredients, { name: "", weight: 0 }]);
    };

    const removeIngredient = (index) => {
        change("ingredients", form.ingredients.filter((_, i) => i !== index));
    };

    /* ============================
       IMAGE FILE HANDLING
       ============================ */
    const handleFiles = (file) => {
        if (!file) return;
        if (prevObjectUrlRef.current) URL.revokeObjectURL(prevObjectUrlRef.current);

        const url = URL.createObjectURL(file);
        prevObjectUrlRef.current = url;

        setForm(prev => ({ ...prev, imageFile: file, imagePreview: url }));
    };

    const onDrop = (e) => {
        e.preventDefault();
        const f = e.dataTransfer?.files?.[0];
        if (f && f.type.startsWith("image/")) handleFiles(f);
        dropRef.current?.classList?.remove("dragover");
    };

    const onDragOver = (e) => {
        e.preventDefault();
        dropRef.current?.classList?.add("dragover");
    };

    const onDragLeave = () => {
        dropRef.current?.classList?.remove("dragover");
    };

    const onSelectFile = (e) => {
        const f = e.target.files?.[0];
        if (f && f.type.startsWith("image/")) handleFiles(f);
    };

    /* ============================
            SUBMIT
       ============================ */
    const submit = () => {
        debugger;
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

        if (editing) onUpdate(editing.id, payload);
        else onCreate(payload);

        onClose();
    };

    if (!open) return null;

    return (
        <div className="bh-modal" onClick={onClose}>
            <div className="bh-modal-card" onClick={e => e.stopPropagation()}>
                <h3>{editing ? "Редагувати" : "Нова страва"}</h3>

                {/* -------------------------
                    PAGE 1 — Info + Image
                ------------------------- */}
                {page === 1 && (
                    <>
                        <div className="modal-row">
                            <label>Назва</label>
                            <input value={form.name} onChange={e => change("name", e.target.value)} />
                        </div>

                        <div className="modal-row">
                            <label>Опис</label>
                            <textarea value={form.description} onChange={e => change("description", e.target.value)} />
                        </div>
                        <select
                            className="bh-select"
                            value={form.category}
                            onChange={(e) => setForm({ ...form, category: Number(e.target.value) })}
                        >
                            {CategoryList.map(cat => (
                                <option key={cat.id} value={cat.id}>{cat.name}</option>
                            ))}
                        </select>
                        <div
                            ref={dropRef}
                            className="bh-droparea"
                            onDrop={onDrop}
                            onDragOver={onDragOver}
                            onDragLeave={onDragLeave}
                            onClick={() => dropRef.current?.querySelector('input[type="file"]')?.click()}
                        >
                            {form.imagePreview ? (
                                <img src={form.imagePreview} alt="preview" />
                            ) : (
                                <div style={{ textAlign: "center" }}>
                                    <div style={{ fontWeight: 700, color: "var(--muted-2)" }}>Перетягніть або оберіть зображення</div>
                                    <div style={{ fontSize: 13, color: "var(--muted-2)" }}>PNG / JPG, до 5MB</div>
                                </div>
                            )}
                            <input type="file" accept="image/*" style={{ display: "none" }} onChange={onSelectFile} />
                        </div>

                        <div className="modal-row row-actions">
                            <button className="btn ghost" onClick={onClose}>Скасувати</button>
                            <button
                                className="btn primary"
                                onClick={() => setPage(2)}
                                disabled={!form.name.trim()}
                            >
                                Далі →
                            </button>
                        </div>
                    </>
                )}

                {/* -------------------------
                    PAGE 2 — Price + Ingredients
                ------------------------- */}
                {page === 2 && (
                    <>
                        <div className="modal-row two">
                            <div>
                                <label>Ціна, ₴</label>
                                <input
                                    type="number"
                                    value={form.price}
                                    onChange={e => change("price", e.target.value)}
                                />
                            </div>

                            <div>
                                <label>Час приготування (хв)</label>
                                <input
                                    type="number"
                                    value={form.cookingTime}
                                    onChange={e => change("cookingTime", e.target.value)}
                                />
                            </div>
                        </div>

                        <h4>Інгредієнти</h4>

                        {form.ingredients.map((ing, i) => (
                            <div key={i} className="ingredient-row">
                                <input
                                    placeholder="Назва"
                                    value={ing.name}
                                    onChange={e => updateIngredient(i, "name", e.target.value)}
                                />
                                <input
                                    type="number"
                                    placeholder="Вага, г"
                                    value={ing.weight}
                                    onChange={e => updateIngredient(i, "weight", Number(e.target.value))}
                                />
                                <button onClick={() => removeIngredient(i)} className="btn danger small">X</button>
                            </div>
                        ))}

                        <button onClick={addIngredient} className="btn ghost">+ Додати інгредієнт</button>

                        <div className="modal-row row-actions">
                            <button className="btn ghost" onClick={() => setPage(1)}>← Назад</button>
                            <button className="btn primary" onClick={submit}>
                                {editing ? "Зберегти" : "Додати"}
                            </button>
                        </div>
                    </>
                )}
            </div>
        </div>
    );
}
