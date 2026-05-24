import { useState } from "react";
import { Banknote, Loader2 } from "lucide-react";
import { createManualPayout } from "../../../api/BusinessDashboard";
import {
    showErrorToast,
    showSuccessToast
} from "../../../global-components/toast/ToastService";
import { formatMoney } from "../utils/formatters";

/**
 * Triggers a manual payout of the connected account's full available
 * balance. Express accounts pay out automatically on Stripe's schedule;
 * this button is for owners who want to drain the balance immediately.
 *
 * Disabled when there's nothing to pay out — server-side checks
 * (`payouts_enabled`, exact balance) catch the rest with a toast.
 */
export default function ManualPayoutButton({ businessId, available, currency, onPayoutCreated }) {
    const [submitting, setSubmitting] = useState(false);

    const hasBalance = Number(available) > 0;
    const disabled = !businessId || !hasBalance || submitting;

    const handleClick = async () => {
        if (disabled) return;
        const confirmed = window.confirm(
            `Pay out ${formatMoney(available, currency)} to your bank now?\n\n` +
            "Stripe normally settles balances automatically on its schedule. " +
            "Use this only if you want the funds immediately."
        );
        if (!confirmed) return;

        setSubmitting(true);
        try {
            const result = await createManualPayout(businessId);
            showSuccessToast(
                `Payout of ${formatMoney(result.amount, result.currency)} initiated. ` +
                "Stripe will deliver it to your bank shortly."
            );
            onPayoutCreated?.(result);
        } catch (err) {
            if (!err?.toastShown) {
                showErrorToast(err?.message ?? "Failed to initiate payout.");
            }
        } finally {
            setSubmitting(false);
        }
    };

    return (
        <button
            type="button"
            className="manual-payout-btn"
            onClick={handleClick}
            disabled={disabled}
            title={
                !hasBalance
                    ? "No available balance — new payments may still be in Stripe's pending window."
                    : `Available: ${formatMoney(available, currency)}`
            }
        >
            {submitting ? <Loader2 size={16} className="manual-payout-btn__spin" /> : <Banknote size={16} />}
            {submitting ? "Initiating…" : "Pay out now"}
        </button>
    );
}
