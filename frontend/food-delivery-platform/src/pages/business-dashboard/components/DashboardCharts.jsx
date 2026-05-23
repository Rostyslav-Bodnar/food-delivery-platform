import {
    Area,
    AreaChart,
    Bar,
    BarChart,
    CartesianGrid,
    Cell,
    Legend,
    Pie,
    PieChart,
    ResponsiveContainer,
    Tooltip,
    XAxis,
    YAxis
} from "recharts";
import { formatCompactMoney, formatDateShort, formatMoney } from "../utils/formatters";

// Design tokens duplicated here because recharts can't resolve CSS vars
// directly and reads colors from inline props/strings at render time.
const COLOR = {
    accent1: "#ff6b6b",   // red — refunds / negatives
    accent2: "#7c5cff",   // purple — primary
    accent3: "#00d4ff",   // cyan — net / secondary
    gold: "#f5b642",      // gold — fees
    muted: "#97a0a9",
    text: "#e6eef6",
    grid: "rgba(255, 255, 255, 0.06)",
    surface: "rgba(11, 15, 20, 0.92)"
};

const BREAKDOWN_COLORS = [COLOR.accent2, COLOR.gold, COLOR.muted, COLOR.accent1];

const tooltipStyle = {
    background: COLOR.surface,
    border: "1px solid rgba(255,255,255,0.08)",
    borderRadius: 10,
    boxShadow: "0 18px 38px rgba(2,6,23,0.55)",
    color: COLOR.text,
    fontSize: 12
};

const MoneyTooltip = ({ active, payload, label, currency }) => {
    if (!active || !payload?.length) return null;
    return (
        <div style={{ ...tooltipStyle, padding: "10px 14px", minWidth: 180 }}>
            <div style={{ color: COLOR.muted, marginBottom: 6, fontSize: 11, letterSpacing: "0.06em", textTransform: "uppercase" }}>
                {label}
            </div>
            {payload.map(p => (
                <div key={p.dataKey} style={{ display: "flex", justifyContent: "space-between", gap: 14, padding: "2px 0" }}>
                    <span style={{ color: p.color, fontWeight: 600, textTransform: "capitalize" }}>
                        {p.dataKey}
                    </span>
                    <span style={{ color: COLOR.text, fontVariantNumeric: "tabular-nums" }}>
                        {formatMoney(p.value, currency)}
                    </span>
                </div>
            ))}
        </div>
    );
};

// =============================================================================
// Income over time — stacked area for gross & net
// =============================================================================

export function IncomeChart({ data, currency }) {
    // Recharts wants string dates for the X axis. Pre-format here so we don't
    // pay the cost in every label render.
    const display = data.map(p => ({
        date: formatDateShort(p.date),
        gross: round(p.gross),
        net: round(p.net),
        refunds: round(p.refunds),
        fees: round(p.fees)
    }));

    return (
        <ResponsiveContainer width="100%" height={280}>
            <AreaChart data={display} margin={{ top: 8, right: 18, left: -10, bottom: 0 }}>
                <defs>
                    <linearGradient id="grossFill" x1="0" y1="0" x2="0" y2="1">
                        <stop offset="0%" stopColor={COLOR.accent2} stopOpacity={0.55} />
                        <stop offset="100%" stopColor={COLOR.accent2} stopOpacity={0} />
                    </linearGradient>
                    <linearGradient id="netFill" x1="0" y1="0" x2="0" y2="1">
                        <stop offset="0%" stopColor={COLOR.accent3} stopOpacity={0.45} />
                        <stop offset="100%" stopColor={COLOR.accent3} stopOpacity={0} />
                    </linearGradient>
                </defs>
                <CartesianGrid stroke={COLOR.grid} vertical={false} />
                <XAxis
                    dataKey="date"
                    tickLine={false}
                    axisLine={{ stroke: COLOR.grid }}
                    tick={{ fill: COLOR.muted, fontSize: 11 }}
                    minTickGap={20}
                />
                <YAxis
                    tickLine={false}
                    axisLine={false}
                    tick={{ fill: COLOR.muted, fontSize: 11 }}
                    tickFormatter={(v) => formatCompactMoney(v, currency)}
                    width={60}
                />
                <Tooltip content={<MoneyTooltip currency={currency} />} cursor={{ stroke: COLOR.accent2, strokeOpacity: 0.18 }} />
                <Legend
                    wrapperStyle={{ paddingTop: 8 }}
                    iconType="circle"
                    formatter={(v) => <span style={{ color: COLOR.muted, fontSize: 12, textTransform: "capitalize" }}>{v}</span>}
                />
                <Area
                    type="monotone"
                    dataKey="gross"
                    stroke={COLOR.accent2}
                    strokeWidth={2.2}
                    fill="url(#grossFill)"
                    activeDot={{ r: 4, strokeWidth: 0 }}
                />
                <Area
                    type="monotone"
                    dataKey="net"
                    stroke={COLOR.accent3}
                    strokeWidth={2.2}
                    fill="url(#netFill)"
                    activeDot={{ r: 4, strokeWidth: 0 }}
                />
            </AreaChart>
        </ResponsiveContainer>
    );
}

// =============================================================================
// Outcome breakdown — donut
// =============================================================================

export function OutcomeBreakdownChart({ data, currency }) {
    if (!data?.length) {
        return <div className="chart-empty">No outcome breakdown yet.</div>;
    }

    const total = data.reduce((acc, d) => acc + d.amount, 0);

    return (
        <div className="donut-shell">
            <ResponsiveContainer width="100%" height={260}>
                <PieChart>
                    <Pie
                        data={data}
                        dataKey="amount"
                        nameKey="category"
                        innerRadius={70}
                        outerRadius={100}
                        paddingAngle={2}
                        stroke="none"
                    >
                        {data.map((_, i) => (
                            <Cell key={i} fill={BREAKDOWN_COLORS[i % BREAKDOWN_COLORS.length]} />
                        ))}
                    </Pie>
                    <Tooltip
                        contentStyle={tooltipStyle}
                        formatter={(value, name) => [formatMoney(value, currency), name]}
                    />
                </PieChart>
            </ResponsiveContainer>

            <div className="donut-legend">
                {data.map((item, i) => {
                    const pct = total > 0 ? (item.amount / total) * 100 : 0;
                    return (
                        <div key={item.category} className="donut-legend__row">
                            <span
                                className="donut-legend__dot"
                                style={{ background: BREAKDOWN_COLORS[i % BREAKDOWN_COLORS.length] }}
                            />
                            <span className="donut-legend__label">{item.category}</span>
                            <span className="donut-legend__value">
                                {formatMoney(item.amount, currency)}
                            </span>
                            <span className="donut-legend__pct">{pct.toFixed(1)}%</span>
                        </div>
                    );
                })}
            </div>
        </div>
    );
}

// =============================================================================
// Per-dish revenue — horizontal bar
// =============================================================================

export function DishRevenueChart({ data, currency, limit = 10 }) {
    if (!data?.length) {
        return <div className="chart-empty">No delivered orders in this window yet.</div>;
    }

    const top = data.slice(0, limit);
    const height = Math.max(top.length * 38 + 32, 180);

    return (
        <ResponsiveContainer width="100%" height={height}>
            <BarChart data={top} layout="vertical" margin={{ top: 8, right: 28, left: 8, bottom: 0 }}>
                <CartesianGrid stroke={COLOR.grid} horizontal={false} />
                <XAxis
                    type="number"
                    tickLine={false}
                    axisLine={false}
                    tick={{ fill: COLOR.muted, fontSize: 11 }}
                    tickFormatter={(v) => formatCompactMoney(v, currency)}
                />
                <YAxis
                    type="category"
                    dataKey="dishName"
                    tickLine={false}
                    axisLine={false}
                    tick={{ fill: COLOR.text, fontSize: 12 }}
                    width={140}
                />
                <Tooltip
                    contentStyle={tooltipStyle}
                    cursor={{ fill: "rgba(124,92,255,0.08)" }}
                    formatter={(value, _name, ctx) => {
                        const row = ctx?.payload;
                        return [
                            <>
                                <div>{formatMoney(value, currency)}</div>
                                {row && (
                                    <div style={{ color: COLOR.muted, fontSize: 11, marginTop: 2 }}>
                                        {row.quantitySold} sold · {row.orderCount} orders
                                    </div>
                                )}
                            </>,
                            "Revenue"
                        ];
                    }}
                />
                <defs>
                    <linearGradient id="dishBar" x1="0" y1="0" x2="1" y2="0">
                        <stop offset="0%" stopColor={COLOR.accent2} />
                        <stop offset="100%" stopColor={COLOR.accent3} />
                    </linearGradient>
                </defs>
                <Bar dataKey="revenue" fill="url(#dishBar)" radius={[0, 6, 6, 0]} />
            </BarChart>
        </ResponsiveContainer>
    );
}

const round = (n) => Math.round((n + Number.EPSILON) * 100) / 100;
