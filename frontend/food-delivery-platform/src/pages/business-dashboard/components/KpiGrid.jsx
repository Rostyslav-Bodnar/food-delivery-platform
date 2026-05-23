import {
    ArrowDownToLine,
    Banknote,
    CreditCard,
    Landmark,
    ReceiptText,
    TrendingDown,
    TrendingUp,
    Wallet
} from "lucide-react";
import { formatMoney } from "../utils/formatters";

const TONES = {
    primary: "is-primary",
    positive: "is-positive",
    negative: "is-negative",
    neutral: "is-neutral",
    info: "is-info"
};

const KpiCard = ({ label, value, currency, icon: Icon, tone = "neutral", hint }) => (
    <article className={`kpi-card ${TONES[tone] ?? TONES.neutral}`}>
        <div className="kpi-card__icon">
            <Icon size={18} strokeWidth={2.2} />
        </div>
        <div className="kpi-card__body">
            <span className="kpi-card__label">{label}</span>
            <strong className="kpi-card__value">{formatMoney(value, currency)}</strong>
            {hint && <span className="kpi-card__hint">{hint}</span>}
        </div>
    </article>
);

export default function KpiGrid({ summary, balance, currency }) {
    return (
        <section className="kpi-grid">
            <KpiCard
                label="Net income"
                value={summary.netIncome}
                currency={currency}
                icon={TrendingUp}
                tone="primary"
                hint={`${summary.orders} orders settled`}
            />
            <KpiCard
                label="Gross sales"
                value={summary.grossSales}
                currency={currency}
                icon={ReceiptText}
                tone="positive"
            />
            <KpiCard
                label="Refunds"
                value={summary.refunds}
                currency={currency}
                icon={TrendingDown}
                tone="negative"
                hint={summary.refundedOrders > 0 ? `${summary.refundedOrders} refunded` : undefined}
            />
            <KpiCard
                label="Platform fee"
                value={summary.platformFees}
                currency={currency}
                icon={Banknote}
                tone="neutral"
            />
            <KpiCard
                label="Stripe fees"
                value={summary.stripeProcessingFees}
                currency={currency}
                icon={CreditCard}
                tone="neutral"
            />
            <KpiCard
                label="Paid to bank"
                value={summary.paidOut}
                currency={currency}
                icon={ArrowDownToLine}
                tone="info"
            />
            <KpiCard
                label="Available"
                value={balance.available}
                currency={balance.currency || currency}
                icon={Wallet}
                tone="info"
                hint="Ready for next payout"
            />
            <KpiCard
                label="Pending"
                value={balance.pending}
                currency={balance.currency || currency}
                icon={Landmark}
                tone="neutral"
                hint="Still settling"
            />
        </section>
    );
}
