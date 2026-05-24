import { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import { CheckCircle2, Loader2 } from "lucide-react";
import { useUser } from "../../context/UserContext";
import "./styles/OnboardingStatusPage.css";

// How long to keep polling for `stripeOnboardedAt` to populate before we
// give up and send the user home anyway. The Stripe webhook race window
// is normally a few seconds; 30s is comfortable headroom on a cold backend.
const POLL_TIMEOUT_MS = 30_000;
const POLL_INTERVAL_MS = 2_000;

/**
 * Landing page Stripe redirects to after Express onboarding succeeds.
 * Polls our profile endpoint until `stripeOnboardedAt` flips non-null
 * (which happens when the `account.updated` webhook lands and the
 * StripeConnectWebhookController updates the row), then redirects to the
 * business home so the gated features unlock automatically.
 */
export default function OnboardingDonePage() {
    const navigate = useNavigate();
    const { accounts, currentAccountId, reloadUser, loading } = useUser();

    // pending → still polling; ready → onboarded confirmed; timeout → gave up
    const [state, setState] = useState("pending");

    const currentAccount = accounts.find((a) => a.id === currentAccountId);
    const isOnboarded = Boolean(currentAccount?.stripeOnboardedAt);

    useEffect(() => {
        if (loading) return;

        if (isOnboarded) {
            setState("ready");
            const timer = setTimeout(() => navigate("/", { replace: true }), 1500);
            return () => clearTimeout(timer);
        }

        const startedAt = Date.now();
        let cancelled = false;

        const tick = async () => {
            if (cancelled) return;
            await reloadUser();
            if (cancelled) return;

            if (Date.now() - startedAt >= POLL_TIMEOUT_MS) {
                // Webhook never landed in our window. Send the user home
                // anyway — the next refetch the home page does will catch
                // up once the webhook finally arrives.
                setState("timeout");
                setTimeout(() => navigate("/", { replace: true }), 2500);
                return;
            }
            setTimeout(tick, POLL_INTERVAL_MS);
        };

        tick();
        return () => { cancelled = true; };
    }, [loading, isOnboarded, reloadUser, navigate]);

    return (
        <div className="onboarding-status-shell">
            <div className="onboarding-status-card">
                {state === "ready" ? (
                    <>
                        <div className="onboarding-status-icon is-success">
                            <CheckCircle2 size={42} strokeWidth={2.2} />
                        </div>
                        <h1>You're all set</h1>
                        <p>
                            Your Stripe account is ready and your business can
                            now accept orders. Taking you back to your home…
                        </p>
                    </>
                ) : state === "timeout" ? (
                    <>
                        <div className="onboarding-status-icon is-warning">
                            <CheckCircle2 size={42} strokeWidth={2.2} />
                        </div>
                        <h1>Almost there</h1>
                        <p>
                            Stripe accepted your details — we're waiting on the
                            final confirmation. Heading home; your account will
                            unlock as soon as it lands.
                        </p>
                    </>
                ) : (
                    <>
                        <div className="onboarding-status-icon is-pending">
                            <Loader2 size={42} strokeWidth={2.2} className="spin" />
                        </div>
                        <h1>Finalising your account</h1>
                        <p>
                            Stripe is finishing up. This usually takes a few
                            seconds — please don't close this page.
                        </p>
                    </>
                )}
            </div>
        </div>
    );
}
