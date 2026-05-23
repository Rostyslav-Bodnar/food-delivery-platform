import { CheckCircle2, Clock3, XCircle } from "lucide-react";
import { formatDateTime, formatMoney } from "../utils/formatters";

const STATUS_META = {
    paid: { label: "Paid", className: "is-paid", icon: CheckCircle2 },
    in_transit: { label: "In transit", className: "is-pending", icon: Clock3 },
    pending: { label: "Pending", className: "is-pending", icon: Clock3 },
    failed: { label: "Failed", className: "is-failed", icon: XCircle },
    canceled: { label: "Canceled", className: "is-failed", icon: XCircle }
};

const StatusBadge = ({ status }) => {
    const meta = STATUS_META[status] ?? { label: status, className: "is-pending", icon: Clock3 };
    const Icon = meta.icon;
    return (
        <span className={`payout-status ${meta.className}`}>
            <Icon size={14} strokeWidth={2.2} />
            {meta.label}
        </span>
    );
};

export default function PayoutsTable({ payouts }) {
    if (!payouts?.length) {
        return (
            <div className="payouts-empty">
                <p>No payouts to the business bank account yet in this window.</p>
                <span>Stripe groups settled balance and pays out automatically every business day once the threshold is reached.</span>
            </div>
        );
    }

    return (
        <div className="payouts-table-shell">
            <table className="payouts-table">
                <thead>
                    <tr>
                        <th>Created</th>
                        <th>Arrival</th>
                        <th>Status</th>
                        <th className="num">Amount</th>
                    </tr>
                </thead>
                <tbody>
                    {payouts.map(p => (
                        <tr key={p.id}>
                            <td>
                                <div className="payout-cell-primary">{formatDateTime(p.createdAtUtc)}</div>
                                <div className="payout-cell-sub">{p.id.slice(0, 14)}…</div>
                            </td>
                            <td>{p.arrivalUtc ? formatDateTime(p.arrivalUtc) : "—"}</td>
                            <td>
                                <StatusBadge status={p.status} />
                                {p.failureMessage && (
                                    <div className="payout-cell-sub payout-failure">{p.failureMessage}</div>
                                )}
                            </td>
                            <td className="num">{formatMoney(p.amount, p.currency)}</td>
                        </tr>
                    ))}
                </tbody>
            </table>
        </div>
    );
}
