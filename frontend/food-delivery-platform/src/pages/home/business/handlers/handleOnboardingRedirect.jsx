// src/pages/business/handlers/useOnboardingRedirect.js
import { useState, useCallback } from "react"
import { getOnboardingLink } from "../../../../api/Account"

const FALLBACK_RETRY_SECONDS = 15;

export default function useOnboardingRedirect() {
    const [loadingOnboarding, setLoadingOnboarding] = useState(false)
    const [error, setError] = useState(null)
    const [provisioning, setProvisioning] = useState(false)
    const [retryAfterSeconds, setRetryAfterSeconds] = useState(null)

    const handleOnboardingRedirect = useCallback(async (businessId) => {
        if (loadingOnboarding) return

        try {
            setLoadingOnboarding(true)
            setError(null)
            setProvisioning(false)
            setRetryAfterSeconds(null)

            const result = await getOnboardingLink(businessId)

            if (result?.status === "provisioning") {
                // StripeAccountProvisioningWorker hasn't populated the
                // Stripe Connect account yet. Surface a waiting state
                // instead of an error and let the user retry.
                const retry = result.retryAfterSeconds ?? FALLBACK_RETRY_SECONDS
                setProvisioning(true)
                setRetryAfterSeconds(retry)
                return
            }

            if (!result?.url) {
                setError("Onboarding link is not available yet. Please try again.")
                return
            }

            // ✅ Redirect only on a ready URL
            window.location.href = result.url
        } catch (err) {
            // ✅ err.message з axios interceptor (Gateway flow)
            setError(err.message || "Failed to start onboarding")
        } finally {
            setLoadingOnboarding(false)
        }
    }, [loadingOnboarding])

    return {
        handleOnboardingRedirect,
        loadingOnboarding,
        error,
        provisioning,
        retryAfterSeconds,
        retry: handleOnboardingRedirect,
        clearError: () => setError(null)
    }
}
