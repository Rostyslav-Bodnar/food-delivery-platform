import {useState} from "react";
import {getOnboardingLink} from "../../../../api/Account.jsx";

export default function useOnboardingRedirect() {
    const [loadingOnboarding, setLoadingOnboarding] = useState(false);

    const handleOnboardingRedirect = async (businessId) => {
        if (loadingOnboarding) return;

        try {
            setLoadingOnboarding(true);
            window.location.href = await getOnboardingLink(businessId);
        } catch (e) {
            console.error("Onboarding error:", e);
        } finally {
            setLoadingOnboarding(false);
        }
    };

    return { handleOnboardingRedirect, loadingOnboarding };
}