import React, { useEffect, useMemo, useState } from "react";
import { Plus, Search, Trash2, Edit3, Filter, Grid, BarChart2, Users } from "lucide-react";
import "./styles/BusinessHomePage.css";
import DishComponent from "./DishComponent";

// 🟢 Імпорт API
import {
    getDishesByBusinessId,
    createDish,
    updateDish,
    deleteDish
} from "../api/Dish.jsx";

const SIDEBAR_ITEMS = [
    { id: "dashboard", label: "Статистика", icon: BarChart2 },
    { id: "orders", label: "Керування замовленнями", icon: Grid },
    { id: "dishes", label: "Керування стравами", icon: Filter },
    { id: "staff", label: "Керування працівниками", icon: Users },
];

export default function BusinessHomePage({ userData }) {
    const [active, setActive] = useState("dishes");
    const [dishes, setDishes] = useState([]);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState(null);

    const token = localStorage.getItem("accessToken");
    const businessId = userData.currentAccount?.id;

    const [q, setQ] = useState("");
    const [category, setCategory] = useState("all");
    const [onlyPopular, setOnlyPopular] = useState(false);
    const [sortBy, setSortBy] = useState("name");

    const [isModalOpen, setIsModalOpen] = useState(false);
    const [editing, setEditing] = useState(null);

    const [toDelete, setToDelete] = useState(null);
    const [showDeleteConfirm, setShowDeleteConfirm] = useState(false);

    // ============================
    // 🔥 LOAD REAL DISHES FROM API
    // ============================
    useEffect(() => {
        const loadDishes = async () => {
            try {
                setLoading(true);

                const res = await getDishesByBusinessId(businessId, token);

                setDishes(res);
            } catch (err) {
                console.error(err);
                setError("Не вдалося завантажити список страв.");
            } finally {
                setLoading(false);
            }
        };

        loadDishes();
    }, [businessId, token]);

    // ============================
    // 🔥 CREATE DISH — API
    // ============================
    const handleCreate = async (newDish) => {
        try {
            const created = await createDish(newDish, token);
            setDishes(prev => [created, ...prev]);
        } catch (err) {
            console.log(newDish);
            console.error(err);
            alert("Помилка створення страви");
        }
    };

    // ============================
    // 🔥 UPDATE DISH — API
    // ============================
    const handleUpdate = async (id, patch) => {
        try {
            const updated = await updateDish(patch, token);

            setDishes(prev =>
                prev.map(d => (d.id === id ? updated : d))
            );
        } catch (err) {
            console.error(err);
            alert("Помилка оновлення страви");
        }
    };

    // ============================
    // 🔥 DELETE DISH — API
    // ============================
    const handleDelete = async (id) => {
        try {
            await deleteDish(id, token);
            setDishes(prev => prev.filter(d => d.id !== id));
        } catch (err) {
            console.error(err);
            alert("Не вдалося видалити страву");
        }

        setShowDeleteConfirm(false);
        setToDelete(null);
    };

    // ============================
    // FILTER & SORT
    // ============================
    const categories = useMemo(() => {
        const setC = new Set(dishes.map(d => d.category));
        return ["all", ...Array.from(setC)];
    }, [dishes]);

    const filtered = useMemo(() => {
        let out = dishes.slice();

        if (q.trim()) out = out.filter(d => d.name.toLowerCase().includes(q.toLowerCase()));
        if (category !== "all") out = out.filter(d => d.category === category);
        if (onlyPopular) out = out.filter(d => d.popular);
        if (sortBy === "name") out.sort((a, b) => a.name.localeCompare(b.name));
        if (sortBy === "price") out.sort((a, b) => a.price - b.price);
        if (sortBy === "rating") out.sort((a, b) => b.rating - a.rating);

        return out;
    }, [dishes, q, category, onlyPopular, sortBy]);

    const openCreate = () => {
        setEditing(null);
        setIsModalOpen(true);
    };

    const openEdit = (dish) => {
        setEditing(dish);
        setIsModalOpen(true);
    };

    return (
        <div className="bh-page">
            {/* SIDEBAR */}
            <aside className="bh-sidebar">
                <div className="bh-brand">
                    <div className="bh-logo">FE</div>
                    <div className="bh-title">FoodExpress</div>
                </div>

                <nav className="bh-nav">
                    {SIDEBAR_ITEMS.map(it => {
                        const Icon = it.icon;
                        return (
                            <button
                                key={it.id}
                                className={`bh-nav-item ${active === it.id ? "active" : ""}`}
                                onClick={() => setActive(it.id)}
                            >
                                <Icon size={18} />
                                <span>{it.label}</span>
                            </button>
                        );
                    })}
                </nav>
            </aside>

            {/* MAIN */}
            <main className="bh-main">
                <header className="bh-top">
                    <h1 className="bh-heading">Керування стравами</h1>

                    <div className="bh-controls">
                        <div className="search-wrap">
                            <Search size={16} className="icon" />
                            <input placeholder="Пошук страв..." value={q} onChange={e => setQ(e.target.value)} />
                        </div>

                        <div className="filters">
                            <select value={category} onChange={e => setCategory(e.target.value)}>
                                {categories.map(c => (
                                    <option key={c} value={c}>
                                        {c === "all" ? "Усі категорії" : c}
                                    </option>
                                ))}
                            </select>

                            <select value={sortBy} onChange={e => setSortBy(e.target.value)}>
                                <option value="name">За назвою</option>
                                <option value="price">За ціною</option>
                                <option value="rating">За рейтингом</option>
                            </select>

                            <label className="popular-toggle">
                                <input type="checkbox" checked={onlyPopular} onChange={e => setOnlyPopular(e.target.checked)} />
                                Хіти
                            </label>
                        </div>
                    </div>

                    <div className="bh-top-cta">
                        <button className="add-dish-btn" onClick={openCreate}>
                            <Plus size={16} /> Додати страву
                        </button>
                    </div>
                </header>

                {/* CONTENT */}
                <section className="bh-content">
                    {loading ? (
                        <div className="bh-empty">Завантаження…</div>
                    ) : error ? (
                        <div className="bh-empty error">{error}</div>
                    ) : filtered.length === 0 ? (
                        <div className="bh-empty">Немає страв</div>
                    ) : (
                        <div className="dishes-grid admin">
                            {filtered.map(d => (
                                <div key={d.id} className="admin-dish-card">
                                    <div className="thumb" style={{ backgroundImage: `url(${d.imageUrl || d.image})` }} />
                                    <div className="meta">
                                        <div className="row">
                                            <h3 className="dish-name">{d.name}</h3>
                                            <div className="price">{d.price} ₴</div>
                                        </div>

                                        <div className="row sub">
                                            <div className="cat">{d.category}</div>
                                            <div className="rating">⭐ {d.rating}</div>
                                            {d.popular && <div className="badge">ХІТ</div>}
                                        </div>

                                        <div className="row actions">
                                            <button className="icon-btn" onClick={() => openEdit(d)} title="Редагувати">
                                                <Edit3 size={16} />
                                            </button>
                                            <button
                                                className="icon-btn danger"
                                                onClick={() => {
                                                    setToDelete(d);
                                                    setShowDeleteConfirm(true);
                                                }}
                                                title="Видалити"
                                            >
                                                <Trash2 size={16} />
                                            </button>
                                        </div>
                                    </div>
                                </div>
                            ))}
                        </div>
                    )}
                </section>

                {/* FOOTER */}
                <footer className="bh-footer">
                    <div>Показано: {filtered.length} з {dishes.length}</div>
                </footer>
            </main>

            {/* MODAL */}
            <DishComponent
                open={isModalOpen}
                onClose={() => setIsModalOpen(false)}
                onCreate={handleCreate}
                onUpdate={handleUpdate}
                editing={editing}
                userData = {userData}
            />

            {/* DELETE CONFIRM */}
            {showDeleteConfirm && toDelete && (
                <div className="bh-confirm">
                    <div className="bh-confirm-card">
                        <h4>Підтвердіть видалення</h4>
                        <p>Ви видаляєте «{toDelete.name}». Це незворотно.</p>

                        <div className="confirm-actions">
                            <button className="btn ghost" onClick={() => setShowDeleteConfirm(false)}>Скасувати</button>
                            <button className="btn danger" onClick={() => handleDelete(toDelete.id)}>Видалити</button>
                        </div>
                    </div>
                </div>
            )}
        </div>
    );
}
