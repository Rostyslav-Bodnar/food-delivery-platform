// src/pages/business/handlers/useOnboardingRedirect.js
import { useState, useCallback } from "react"
import { getOnboardingLink } from "../../../../api/Account"

export default function useOnboardingRedirect() {
    const [loadingOnboarding, setLoadingOnboarding] = useState(false)
    const [error, setError] = useState(null)

    const handleOnboardingRedirect = useCallback(async (businessId) => {
        if (loadingOnboarding) return

        try {
            setLoadingOnboarding(true)
            setError(null)

            const url = await getOnboardingLink(businessId)

            // ✅ Redirect only on success
            window.location.href = url
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
        retry: handleOnboardingRedirect,
        clearError: () => setError(null)
    }
}