// Reply for GET /account/onboarding/{businessId}.
// 200 → status="ready" with the Stripe-hosted onboarding URL.
// 202 → status="provisioning"; Stripe Connect account isn't ready yet
//       (StripeAccountProvisioningWorker is in flight). Retry after the hint.
export interface OnboardingLinkResponse {
    status: "ready" | "provisioning"
    url?: string | null
    retryAfterSeconds?: number | null
}
