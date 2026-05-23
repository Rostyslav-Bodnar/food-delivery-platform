import React from "react";
import { Search, ArrowUpDown } from "lucide-react";
import "./OrdersFilterBar.css";

/**
 * Shared filter / sort / search bar used by both customer- and business-orders
 * pages. Stays consistent with the app's accent palette (--accent-2 / --accent-3).
 *
 * Props
 * - statuses: [{ key, label, color? }] — first entry is treated as "all"
 * - activeStatus
 * - onStatusChange(key)
 * - sortOptions: [{ value, label }]
 * - sort
 * - onSortChange(value)
 * - search
 * - onSearchChange(string)
 * - searchPlaceholder
 * - count: optional number displayed in the corner
 */
export default function OrdersFilterBar({
    statuses,
    activeStatus,
    onStatusChange,
    sortOptions,
    sort,
    onSortChange,
    search,
    onSearchChange,
    searchPlaceholder = "Search…",
    count
}) {
    return (
        <div className="ofb">
            <div className="ofb-row ofb-row--top">
                <div className="ofb-search">
                    <Search size={16} className="ofb-search__icon" />
                    <input
                        type="text"
                        value={search ?? ""}
                        onChange={(e) => onSearchChange?.(e.target.value)}
                        placeholder={searchPlaceholder}
                        className="ofb-search__input"
                    />
                    {search && (
                        <button
                            type="button"
                            className="ofb-search__clear"
                            onClick={() => onSearchChange?.("")}
                            aria-label="Clear search"
                        >
                            ×
                        </button>
                    )}
                </div>

                <div className="ofb-sort">
                    <ArrowUpDown size={15} className="ofb-sort__icon" />
                    <select
                        value={sort}
                        onChange={(e) => onSortChange?.(e.target.value)}
                        className="ofb-sort__select"
                    >
                        {sortOptions?.map((opt) => (
                            <option key={opt.value} value={opt.value}>
                                {opt.label}
                            </option>
                        ))}
                    </select>
                </div>

                {typeof count === "number" && (
                    <div className="ofb-count">
                        <strong>{count}</strong>
                        <span>{count === 1 ? "order" : "orders"}</span>
                    </div>
                )}
            </div>

            <div className="ofb-row ofb-row--chips">
                {statuses?.map((s) => {
                    const isActive = activeStatus === s.key;
                    return (
                        <button
                            key={s.key}
                            type="button"
                            onClick={() => onStatusChange?.(s.key)}
                            className={`ofb-chip ${isActive ? "is-active" : ""}`}
                            style={isActive && s.color ? { boxShadow: `0 0 0 1px ${s.color}55, 0 4px 14px ${s.color}33` } : undefined}
                        >
                            {s.color && !isActive && (
                                <span className="ofb-chip__dot" style={{ background: s.color }} />
                            )}
                            {s.label}
                        </button>
                    );
                })}
            </div>
        </div>
    );
}
