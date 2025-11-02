import React, { useEffect, useRef, useState } from "react";

export default function DishComponent({ open, onClose, onCreate, onUpdate, editing }) {
    const [form, setForm] = useState({
        name: "",
        category: "pizza",
        price: "",
        popular: false,
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
                popular: editing.popular ?? false,
                imageFile: null,
                imagePreview: editing.image || ""
            });
        } else {
            setForm({ name: "", category: "pizza", price: "", popular: false, imageFile: null, imagePreview: "" });
        }
        return () => {
            // cleanup any created object URL when modal unmounts or editing changes
            if (prevObjectUrlRef.current) {
                URL.revokeObjectURL(prevObjectUrlRef.current);
                prevObjectUrlRef.current = null;
            }
        };
    }, [editing, open]);

    useEffect(() => {
        if (!open) return;
        const onKey = (e) => {
            if (e.key === "Escape") onClose();
        };
        window.addEventListener("keydown", onKey);
        return () => window.removeEventListener("keydown", onKey);
    }, [open, onClose]);

    if (!open) return null;

    const change = (k, v) => setForm(prev => ({ ...prev, [k]: v }));

    const handleFiles = (file) => {
        if (!file) return;
        // revoke previous object URL
        if (prevObjectUrlRef.current) {
            URL.revokeObjectURL(prevObjectUrlRef.current);
            prevObjectUrlRef.current = null;
        }
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
    const onDragOver = (e) => { e.preventDefault(); dropRef.current?.classList?.add("dragover"); };
    const onDragLeave = () => { dropRef.current?.classList?.remove("dragover"); };

    const onSelectFile = (e) => {
        const f = e.target.files?.[0];
        if (f && f.type.startsWith("image/")) handleFiles(f);
    };

    const submit = () => {
        if (!form.name.trim()) return alert("Вкажіть назву страви");
        const payload = {
            name: form.name.trim(),
            category: form.category,
            price: Number(form.price) || 0,
            popular: !!form.popular
        };
        if (form.imageFile) payload._file = form.imageFile;
        else if (form.imagePreview) payload.image = form.imagePreview;

        if (editing) onUpdate(editing.id, payload);
        else onCreate(payload);

        onClose();
    };

    // Click on overlay should close; click inside card should stop propagation
    const onOverlayClick = () => onClose();
    const onCardClick = (e) => e.stopPropagation();

    return (
        <div
            className="bh-modal"
            role="dialog"
            aria-modal="true"
            onClick={onOverlayClick}
        >
            <div className="bh-modal-card" onClick={onCardClick}>
                <h3>{editing ? "Редагувати страву" : "Нова страва"}</h3>

                <div className="modal-row">
                    <label>Назва</label>
                    <input value={form.name} onChange={e => change("name", e.target.value)} />
                </div>

                <div className="modal-row">
                    <label>Категорія</label>
                    <select value={form.category} onChange={e => change("category", e.target.value)}>
                        <option value="pizza">Піца</option>
                        <option value="burger">Бургер</option>
                        <option value="sushi">Суші</option>
                        <option value="pasta">Паста</option>
                        <option value="salad">Салат</option>
                        <option value="street">Стрітфуд</option>
                    </select>
                </div>

                <div className="modal-row two">
                    <div>
                        <label>Ціна, ₴</label>
                        <input type="number" value={form.price} onChange={e => change("price", e.target.value)} />
                    </div>

                    <div>
                        <label>Хіт</label>
                        <div style={{ display: "flex", alignItems: "center", gap: 8 }}>
                            <input type="checkbox" checked={form.popular} onChange={e => change("popular", e.target.checked)} />
                            <span className="hint">Показувати як хіт</span>
                        </div>
                    </div>
                </div>

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
                    <div className="spacer" />
                    <div className="modal-buttons">
                        <button className="btn ghost" onClick={onClose}>Скасувати</button>
                        <button className="btn primary" onClick={submit}>{editing ? "Зберегти" : "Додати"}</button>
                    </div>
                </div>
            </div>
        </div>
    );
}
