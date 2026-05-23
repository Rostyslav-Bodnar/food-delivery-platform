import { api } from "./apiClient"
import type { ApiResponse } from "../models/responses/Response"
import type {
    AccountResponse,
    BusinessAccountResponse
} from "../models/responses/accounts/AccountResponse"
import type { OnboardingLinkResponse } from "../models/responses/accounts/OnboardingLinkResponse"

// =========================
// GET single account
// =========================
export async function getAccount(
    userId: string
): Promise<AccountResponse> {
    const res = await api.get<ApiResponse<AccountResponse>>(
        `/account/${userId}`
    )

    return res.data.data!
}

// =========================
// GET all accounts of user
// =========================
export async function getAccounts(
    userId: string
): Promise<AccountResponse[]> {
    const res = await api.get<ApiResponse<AccountResponse[]>>(
        `/account/all/${userId}`
    )

    return res.data.data!
}

// =========================
// GET all business accounts
// =========================
export async function getAllBusinessAccounts(): Promise<BusinessAccountResponse[]> {
    const res = await api.get<ApiResponse<BusinessAccountResponse[]>>(
        `/account/all/business`
    )

    return res.data.data!
}

// =========================
// CREATE account (multipart)
// =========================
export async function createAccount(
    accountType: "customer" | "business" | "courier",
    accountData: Record<string, unknown>
): Promise<AccountResponse> {
    const formData = new FormData()

    Object.entries(accountData).forEach(([key, value]) => {
        if (value !== null && value !== undefined) {
            formData.append(key, value as Blob | string)
        }
    })

    const res = await api.post<ApiResponse<AccountResponse>>(
        `/account/${accountType}`,
        formData,
        {
            headers: {
                "Content-Type": "multipart/form-data"
            }
        }
    )

    return res.data.data!
}

// =========================
// UPDATE account
// =========================
export async function updateAccount(
    accountType: "customer" | "business" | "courier",
    accountData: Record<string, unknown>
): Promise<AccountResponse> {
    const formData = new FormData()

    Object.entries(accountData).forEach(([key, value]) => {
        if (value !== null && value !== undefined) {
            formData.append(key, value as Blob | string)
        }
    })

    const res = await api.put<ApiResponse<AccountResponse>>(
        `/account/${accountType}`,
        formData,
        {
            headers: {
                "Content-Type": "multipart/form-data"
            }
        }
    )

    return res.data.data!
}

// =========================
// DELETE account
// =========================
export async function deleteAccount(
    id: string
): Promise<void> {
    await api.delete(`/account/${id}`)
}

// =========================
// GET onboarding link
// Returns { status: "ready", url } on 200, or { status: "provisioning",
// retryAfterSeconds } on 202 while Stripe Connect is still being provisioned
// in the background.
// =========================
export async function getOnboardingLink(
    businessId: string
): Promise<OnboardingLinkResponse> {
    const res = await api.get<ApiResponse<OnboardingLinkResponse>>(
        `/account/onboarding/${businessId}`
    )

    return res.data.data!
}