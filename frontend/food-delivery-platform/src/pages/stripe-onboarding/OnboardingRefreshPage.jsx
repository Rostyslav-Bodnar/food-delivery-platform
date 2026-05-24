import { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import { AlertTriangle, Loader2 } from "lucide-react";
import { useUser } from "../../context/UserContext";
import { getOnboardingLink } from "../../api/Account";
import "./styles/OnboardingStatusPage.css";

/**
 * Landing page Stripe redirects to when the hosted Express onboarding
 * session expires before the user finishes (the original `account_link`
 * URL is single-use and time-limited). We mint a fresh link and bounce
 * them back to Stripe so they can pick up where they left off.
 */
export default function OnboardingRefreshPage() {
    const navigate = useNavigate();
    const { accounts, currentAccountId, loading } = useUser();
    const [error, setError] = useState(null);
    const [provisioning, setProvisioning] = useState(false);

    const currentAccount = accounts.find((a) => a.id === currentAccountId);
    const businessId = currentAccount?.accountType === "Business"
        ? currentAccount.id
        : null;

    useEffect(() => {
        if (loading) return;

        if (!businessId) {
            setError(
                "We couldn't identify the business account to resume. " +
                "Please sign in and try again from your business home."
            );
            return;
        }

        let cancelled = false;

        const requestLink = async () => {
            try {
                setError(null);
                setProvisioning(false);

                const result = await getOnboardingLink(businessId);
                if (cancelled) return;

                if (result?.status === "provisioning") {
                    // Stripe account is still being created by the background
                    // worker — wait and retry on the user's behalf.
                    setProvisioning(true);
                    const wait = (result.retryAfterSeconds ?? 15) * 1000;
                    setTimeout(() => { if (!cancelled) requestLink(); }, wait);
                    return;
                }

                if (!result?.url) {
                    setError("Onboarding link is not available yet. Please try again.");
                    return;
                }

                // Off to Stripe — replaces the SPA, no need to navigate back.
                window.location.href = result.url;
            } catch (err) {
                if (cancelled) return;
                setError(err?.message ?? "Failed to resume onboarding.");
            }
        };

        requestLink();
        return () => { cancelled = true; };
    }, [loading, businessId]);

    return (
        <div className="onboarding-status-shell">
            <div className="onboarding-status-card">
                {error ? (
                    <>
                        <div className="onboarding-status-icon is-error">
                            <AlertTriangle size={42} strokeWidth={2.2} />
                        </div>
                        <h1>Couldn't continue onboarding</h1>
                        <p>{error}</p>
                        <button
                            type="button"
                            className="onboarding-status-button"
                            onClick={() => navigate("/")}
                        >
                            Go back home
                        </button>
                    </>
                ) : provisioning ? (
                    <>
                        <div className="onboarding-status-icon is-pending">
                            <Loader2 size={42} strokeWidth={2.2} className="spin" />
                        </div>
                        <h1>Provisioning your Stripe account</h1>
                        <p>
                            Almost ready — we're waiting on Stripe to finish
                            setting things up. This page will redirect you
                            automatically.
                        </p>
                    </>
                ) : (
                    <>
                        <div className="onboarding-status-icon is-pending">
                            <Loader2 size={42} strokeWidth={2.2} className="spin" />
                        </div>
                        <h1>Resuming your onboarding</h1>
                        <p>
                            Generating a fresh Stripe link — you'll be
                            redirected in a moment.
                        </p>
                    </>
                )}
            </div>
        </div>
    );
}
