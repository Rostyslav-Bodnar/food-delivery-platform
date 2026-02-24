// src/pages/components/BusinessHeader.jsx
import React from "react";
import { Plus, Search } from "lucide-react";
import { CategoryList } from "../../../../constants/category.jsx";

const BusinessHeader = ({
                            q,
                            setQ,
                            category,
                            setCategory,
                            sortBy,
                            setSortBy,
                            onlyPopular,
                            setOnlyPopular,
                            openCreate
                        }) => {
    return (
        <header className="bh-top">
            <h1 className="bh-heading">Dish Management</h1>
            <div className="bh-controls">
                <div className="search-wrap">
                    <Search size={16} className="icon" />
                    <input
                        placeholder="Search dishes..."
                        value={q}
                        onChange={e => setQ(e.target.value)}
                    />
                </div>
                <div className="filters">
                    <select
                        value={category}
                        onChange={e => setCategory(Number(e.target.value))}
                    >
                        <option value="all">All Categories</option>
                        {CategoryList.map(cat => (
                            <option key={cat.id} value={cat.id}>
                                {cat.name}
                            </option>
                        ))}
                    </select>
                    <select
                        value={sortBy}
                        onChange={e => setSortBy(e.target.value)}
                    >
                        <option value="name">By Name</option>
                        <option value="price">By Price</option>
                        <option value="rating">By Rating</option>
                    </select>
                    <label className="popular-toggle">
                        <input
                            type="checkbox"
                            checked={onlyPopular}
                            onChange={e => setOnlyPopular(e.target.checked)}
                        />
                        Popular
                    </label>
                </div>
            </div>
            <div className="bh-top-cta">
                <button className="add-dish-btn" onClick={openCreate}>
                    <Plus size={16} /> Add Dish
                </button>
            </div>
        </header>
    );
};

export default BusinessHeader;