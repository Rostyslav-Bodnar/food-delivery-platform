// Currency formatter with sane fallbacks. We default to USD if the
// backend hasn't echoed the currency yet (initial load before Stripe call).
export function formatMoney(amount, currency = "USD") {
    const value = Number.isFinite(amount) ? amount : 0;
    try {
        return new Intl.NumberFormat("en-US", {
            style: "currency",
            currency: (currency || "USD").toUpperCase(),
            maximumFractionDigits: 2
        }).format(value);
    } catch {
        return `${value.toFixed(2)} ${currency || "USD"}`;
    }
}

export function formatCompactMoney(amount, currency = "USD") {
    const value = Number.isFinite(amount) ? amount : 0;
    try {
        return new Intl.NumberFormat("en-US", {
            style: "currency",
            currency: (currency || "USD").toUpperCase(),
            notation: "compact",
            maximumFractionDigits: 1
        }).format(value);
    } catch {
        return `${value.toFixed(0)} ${currency || "USD"}`;
    }
}

export function formatDateShort(iso) {
    if (!iso) return "";
    const d = new Date(iso);
    return d.toLocaleDateString("en-US", { month: "short", day: "numeric" });
}

export function formatDateTime(iso) {
    if (!iso) return "";
    return new Date(iso).toLocaleString("en-US", {
        month: "short",
        day: "numeric",
        hour: "2-digit",
        minute: "2-digit"
    });
}
