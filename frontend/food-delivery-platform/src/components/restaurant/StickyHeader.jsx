import { Search, X } from "lucide-react";
import "./styles/StickyHeader.css";

const StickyHeader = ({
                          restaurantName,
                          searchQuery,
                          onSearchChange,
                          onClearSearch,
                          sortBy,
                          onSortChange,
                          categories,
                          selectedCategory,
                          onCategorySelect
                      }) => {
    return (
        <div className="sticky-header">
            <div className="search-input-wrapper">
                <Search size={22} className="restaurant-search-icon" />

                <input
                    type="text"
                    placeholder={`Пошук в ${restaurantName}...`}
                    value={searchQuery}
                    onChange={(e) => onSearchChange(e.target.value)}
                />

                {searchQuery && (
                    <X
                        size={22}
                        className="clear-search"
                        onClick={onClearSearch}
                    />
                )}

                <div className="sort-bar">
                    <select value={sortBy} onChange={(e) => onSortChange(e.target.value)}>
                        <option value="popular">Popular</option>
                        <option value="price-asc">Lowest price first</option>
                        <option value="price-desc">Highest price first</option>
                        <option value="rating">By rating</option>
                    </select>
                </div>
            </div>

            <div className="categories-scroll">
                {categories.map(cat => (
                    <button
                        key={cat}
                        onClick={() => onCategorySelect(cat)}
                        className={`category-btn ${selectedCategory === cat ? "active" : ""}`}
                    >
                        {cat === "all" ? "Усе меню" : cat}
                    </button>
                ))}
            </div>
        </div>
    );
};

export default StickyHeader;
