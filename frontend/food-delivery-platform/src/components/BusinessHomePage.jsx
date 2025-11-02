import React, { useEffect, useMemo, useState } from "react";
import { Plus, Search, Trash2, Edit3, Filter, Grid, BarChart2, Users } from "lucide-react";
import "./styles/BusinessHomePage.css";
import DishComponent from "./DishComponent";

/* Mock data — replace with API calls */
const mockInitial = [
    { id: 1, name: "Маргарита Піца", category: "pizza", price: 249, rating: 4.8, popular: true, image: "https://images.unsplash.com/photo-1565299624946-b28f40a0ae38?w=800" },
    { id: 2, name: "Бургер з яловичиною", category: "burger", price: 189, rating: 4.9, popular: true, image: "https://images.unsplash.com/photo-1568901346375-23c9450c58cd?w=800" },
    { id: 3, name: "Паста Карбонара", category: "pasta", price: 219, rating: 4.6, popular: false, image: "https://images.unsplash.com/photo-1621996346565-e3dbc92e08ee?w=800" },
    { id: 4, name: "Салат Цезар", category: "salad", price: 179, rating: 4.4, popular: false, image: "https://images.unsplash.com/photo-1512621776951-a57141f2eefd?w=800" },
];

const SIDEBAR_ITEMS = [
    { id: "dashboard", label: "Статистика", icon: BarChart2 },
    { id: "orders", label: "Керування замовленнями", icon: Grid },
    { id: "dishes", label: "Керування стравами", icon: Filter },
    { id: "staff", label: "Керування працівниками", icon: Users },
];

export default function BusinessHomePage() {
    const [active, setActive] = useState("dishes");
    const [dishes, setDishes] = useState([]);
    const [loading, setLoading] = useState(true);

    const [q, setQ] = useState("");
    const [category, setCategory] = useState("all");
    const [onlyPopular, setOnlyPopular] = useState(false);
    const [sortBy, setSortBy] = useState("name"); // name / price / rating

    const [isModalOpen, setIsModalOpen] = useState(false);
    const [editing, setEditing] = useState(null);

    const [toDelete, setToDelete] = useState(null);
    const [showDeleteConfirm, setShowDeleteConfirm] = useState(false);

    useEffect(() => {
        setTimeout(() => {
            setDishes(mockInitial);
            setLoading(false);
        }, 200);
    }, []);

    const categories = useMemo(() => {
        const setC = new Set(dishes.map(d => d.category));
        return ["all", ...Array.from(setC)];
    }, [dishes]);

    const filtered = useMemo(() => {
        let out = dishes.slice();
        if (q.trim()) {
            const t = q.toLowerCase();
            out = out.filter(d => d.name.toLowerCase().includes(t));
        }
        if (category !== "all") out = out.filter(d => d.category === category);
        if (onlyPopular) out = out.filter(d => d.popular);
        if (sortBy === "name") out.sort((a,b)=> a.name.localeCompare(b.name));
        if (sortBy === "price") out.sort((a,b)=> a.price - b.price);
        if (sortBy === "rating") out.sort((a,b)=> b.rating - a.rating);
        return out;
    }, [dishes, q, category, onlyPopular, sortBy]);

    // CRUD handlers (mock)
    const handleCreate = async (newDish) => {
        const id = Date.now();
        let imageUrl = newDish.image || "https://images.unsplash.com/photo-1546549039-2b7a1a299d3b?w=800";
        if (newDish._file) imageUrl = URL.createObjectURL(newDish._file);
        setDishes(prev => [{ ...newDish, id, image: imageUrl }, ...prev]);
    };

    const handleUpdate = async (id, patch) => {
        let imageUrl = patch.image;
        if (patch._file) imageUrl = URL.createObjectURL(patch._file);
        setDishes(prev => prev.map(d => d.id === id ? { ...d, ...patch, image: imageUrl ?? d.image } : d));
    };

    const handleDelete = async (id) => {
        setDishes(prev => prev.filter(d => d.id !== id));
        setShowDeleteConfirm(false);
        setToDelete(null);
    };

    const openCreate = () => { setEditing(null); setIsModalOpen(true); };
    const openEdit = (dish) => { setEditing(dish); setIsModalOpen(true); };

    return (
        <div className="bh-page">
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

            <main className="bh-main">
                <header className="bh-top">
                    <h1 className="bh-heading">Керування стравами</h1>

                    <div className="bh-controls">
                        <div className="search-wrap">
                            <Search size={16} className="icon" />
                            <input placeholder="Пошук страв за назвою..." value={q} onChange={e=>setQ(e.target.value)} />
                        </div>

                        <div className="filters">
                            <select value={category} onChange={e=>setCategory(e.target.value)}>
                                {categories.map(c => <option key={c} value={c}>{c === "all" ? "Усі категорії" : c}</option>)}
                            </select>

                            <select value={sortBy} onChange={e=>setSortBy(e.target.value)}>
                                <option value="name">За назвою</option>
                                <option value="price">За ціною</option>
                                <option value="rating">За рейтингом</option>
                            </select>

                            <label className="popular-toggle">
                                <input type="checkbox" checked={onlyPopular} onChange={e=>setOnlyPopular(e.target.checked)} />
                                Хіти
                            </label>
                        </div>
                    </div>

                    <div className="bh-top-cta">
                        <button className="add-dish-btn" onClick={openCreate}><Plus size={16} /> Додати страву</button>
                    </div>
                </header>

                <section className="bh-content">
                    {loading ? (
                        <div className="bh-empty">Завантаження…</div>
                    ) : filtered.length === 0 ? (
                        <div className="bh-empty">Немає страв (спробуйте інший фільтр або додайте нову)</div>
                    ) : (
                        <div className="dishes-grid admin">
                            {filtered.map(d => (
                                <div key={d.id} className="admin-dish-card">
                                    <div className="thumb" style={{backgroundImage:`url(${d.image})`}} />
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
                                            <button className="icon-btn" onClick={()=>openEdit(d)} title="Редагувати"><Edit3 size={16} /></button>
                                            <button className="icon-btn danger" onClick={()=>{ setToDelete(d); setShowDeleteConfirm(true); }} title="Видалити"><Trash2 size={16} /></button>
                                        </div>
                                    </div>
                                </div>
                            ))}
                        </div>
                    )}
                </section>

                <footer className="bh-footer">
                    <div>Показано: {filtered.length} з {dishes.length}</div>
                    <div className="small-muted">Підказка: використайте пошук та фільтри для швидкого знаходження</div>
                </footer>
            </main>

            <DishComponent
                open={isModalOpen}
                onClose={() => setIsModalOpen(false)}
                onCreate={handleCreate}
                onUpdate={handleUpdate}
                editing={editing}
            />

            {showDeleteConfirm && toDelete && (
                <div className="bh-confirm">
                    <div className="bh-confirm-card">
                        <h4>Підтвердіть видалення</h4>
                        <p>Ви видаляєте «{toDelete.name}». Ця дія незворотна.</p>
                        <div className="confirm-actions">
                            <button className="btn ghost" onClick={()=>{ setShowDeleteConfirm(false); setToDelete(null); }}>Скасувати</button>
                            <button className="btn danger" onClick={()=>handleDelete(toDelete.id)}>Видалити</button>
                        </div>
                    </div>
                </div>
            )}
        </div>
    );
}
