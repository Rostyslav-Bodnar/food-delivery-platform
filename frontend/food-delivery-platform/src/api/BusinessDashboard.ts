import { api } from "./apiClient"

import type { ApiResponse } from "../models/responses/Response"
import type {
    BusinessDashboardResponse,
    DishRevenue,
    ManualPayoutResponse
} from "../models/responses/business-dashboard/BusinessDashboardResponse"

const toIso = (d: Date) => d.toISOString()

/**
 * Stripe-backed live dashboard for the given business. Window is
 * inclusive of both ends; backend caps at 366 days.
 */
export async function getBusinessDashboard(
    businessId: string,
    from: Date,
    to: Date
): Promise<BusinessDashboardResponse> {
    const res = await api.get<ApiResponse<BusinessDashboardResponse>>(
        `/account/business/${businessId}/dashboard`,
        { params: { from: toIso(from), to: toIso(to) } }
    )
    return res.data.data!
}

/**
 * Per-dish revenue for delivered orders inside the window. Joined locally
 * with the dashboard's Stripe-side numbers — gives the business a feel for
 * which dishes drove the gross sales total.
 */
export async function getRevenueByDish(
    businessId: string,
    from: Date,
    to: Date
): Promise<DishRevenue[]> {
    const res = await api.get<ApiResponse<DishRevenue[]>>(
        `/order/business/${businessId}/revenue-by-dish`,
        { params: { from: toIso(from), to: toIso(to) } }
    )
    return res.data.data!
}

/**
 * Drains the connected account's full available balance to the
 * business's bank. Stripe normally pays out automatically on a daily
 * schedule — this is the manual override.
 */
export async function createManualPayout(
    businessId: string
): Promise<ManualPayoutResponse> {
    const res = await api.post<ApiResponse<ManualPayoutResponse>>(
        `/account/business/${businessId}/payouts/manual`
    )
    return res.data.data!
}
