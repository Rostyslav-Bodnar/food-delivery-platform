// src/pages/components/DishesContent.jsx
import React from "react";
import { Trash2, Edit3 } from "lucide-react";
import { CategoryMap } from "../../../../constants/category";

const DishesContent = ({
                           loading,
                           error,
                           filtered,
                           openEdit,
                           setToDelete,
                           setShowDeleteConfirm
                       }) => {
    return (
        <section className="bh-content">
            {loading ? (
                <div className="bh-empty">Loading…</div>
            ) : error ? (
                <div className="bh-empty error">{error}</div>
            ) : filtered.length === 0 ? (
                <div className="bh-empty">No dishes found</div>
            ) : (
                <div className="dishes-grid admin">
                    {filtered.map(d => (
                        <div key={d.id} className="admin-dish-card">
                            <div
                                className="thumb"
                                style={{ backgroundImage: `url(${d.imageUrl || d.image})` }}
                            />
                            <div className="meta">
                                <div className="row">
                                    <h3 className="dish-name">{d.name}</h3>
                                    <div className="price">{d.price} ₴</div>
                                </div>
                                <div className="row sub">
                                    <div className="cat">{CategoryMap[d.category]}</div>
                                    <div className="rating">⭐ {d.rating}</div>
                                    {d.popular && <div className="badge">HOT</div>}
                                </div>
                                <div className="row actions">
                                    <button
                                        className="icon-btn"
                                        onClick={() => openEdit(d)}
                                        title="Edit"
                                    >
                                        <Edit3 size={16} />
                                    </button>
                                    <button
                                        className="icon-btn danger"
                                        onClick={() => {
                                            setToDelete(d);
                                            setShowDeleteConfirm(true);
                                        }}
                                        title="Delete"
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
    );
};

export default DishesContent;