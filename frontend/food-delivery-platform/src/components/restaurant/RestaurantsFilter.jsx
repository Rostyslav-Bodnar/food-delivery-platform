import { Search, X, Filter } from "lucide-react";
import { CategoryList } from "../../constants/category.jsx";
import "./styles/RestaurantsFilter.css";

const RestaurantsFilter = ({
                               searchQuery,
                               onSearchChange,
                               onClearSearch,
                               selectedCategory,
                               onCategoryChange,
                               sortBy,
                               onSortChange
                           }) => {
    return (
        <div className="search-and-filters">
            <div className="search-bar">
                <Search size={20} className="restaurants-search-icon" />
                <input
                    type="text"
                    placeholder="Search establishment..."
                    value={searchQuery}
                    onChange={(e) => onSearchChange(e.target.value)}
                />
                {searchQuery && (
                    <X
                        size={20}
                        className="clear-search"
                        onClick={onClearSearch}
                    />
                )}
            </div>

            <div className="filters-group">
                <div className="filter-item">
                    <Filter size={18} />
                    <select
                        value={selectedCategory}
                        onChange={(e) => onCategoryChange(e.target.value)}
                    >
                        <option value="all">All categories</option>
                        {CategoryList.map(cat => (
                            <option key={cat.id} value={cat.id}>
                                {cat.name}
                            </option>
                        ))}
                    </select>
                </div>

                <div className="filter-item">
                    <select value={sortBy} onChange={(e) => onSortChange(e.target.value)}>
                        <option value="rating">Rating</option>
                        <option value="time">Fast delivery</option>
                        <option value="name">Name</option>
                    </select>
                </div>
            </div>
        </div>
    );
};

export default RestaurantsFilter;
