import React from "react";
import { Calendar, MapPin, Receipt, ChevronRight } from "lucide-react";

const formatCurrency = (value) =>
    new Intl.NumberFormat("uk-UA", {
        style: "currency",
        currency: "UAH",
        maximumFractionDigits: 0
    }).format(Number(value ?? 0));

/**
 * Read-only order card for the history pages. Same visual language as the
 * customer "active order" card but stripped of mutation actions (no track,
 * cancel, confirm). Status pill, items snippet, total, and a "Details" link.
 */
export default function OrderHistoryCard({ order, statusMeta, onOpenDetails }) {
    const itemsToShow = (order.items ?? []).slice(0, 3);
    const extraCount = Math.max((order.items?.length ?? 0) - itemsToShow.length, 0);
    const total = order.total ?? order.totalPrice ?? 0;

    return (
        <article className="history-card">
            <header className="history-card__head">
                <span className="history-card__id">#{String(order.id ?? "").slice(0, 8)}</span>
                {statusMeta && (
                    <span
                        className="history-card__status"
                        style={statusMeta.color ? { color: statusMeta.color } : undefined}
                    >
                        {statusMeta.icon}
                        {statusMeta.text ?? statusMeta.label ?? order.status}
                    </span>
                )}
            </header>

            <div className="history-card__body">
                <h3 className="history-card__title">
                    {order.restaurant ?? order.businessName ?? "Order"}
                </h3>

                <div className="history-card__meta">
                    {order.createdAt && (
                        <span className="history-card__pill">
                            <Calendar size={12} />
                            {order.createdAt}
                        </span>
                    )}
                    {order.address && (
                        <span className="history-card__pill">
                            <MapPin size={12} />
                            {order.address}
                        </span>
                    )}
                </div>

                {itemsToShow.length > 0 && (
                    <ul className="history-card__items">
                        {itemsToShow.map((item, idx) => (
                            <li
                                key={item.id ?? item.name ?? idx}
                                className="history-card__item"
                            >
                                <span className="history-card__item-name">
                                    {item.name ?? item.dishName ?? "—"}
                                </span>
                                <span className="history-card__item-qty">
                                    ×{item.quantity ?? 1}
                                </span>
                            </li>
                        ))}
                        {extraCount > 0 && (
                            <li className="history-card__item history-card__item--more">
                                +{extraCount} more
                            </li>
                        )}
                    </ul>
                )}
            </div>

            <footer className="history-card__foot">
                <div className="history-card__total">
                    <Receipt size={14} />
                    <span>{formatCurrency(total)}</span>
                </div>
                <button
                    type="button"
                    className="history-card__details-btn"
                    onClick={() => onOpenDetails?.(order)}
                >
                    Details <ChevronRight size={16} />
                </button>
            </footer>
        </article>
    );
}
