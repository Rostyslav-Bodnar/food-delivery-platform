import { api } from "./apiClient"

import type { ApiResponse } from "../models/responses/Response"

import type {
    BusinessLocationResponse
} from "../models/responses/tracking/BusinessLocationResponse"

import type {
    CreateBusinessLocationRequest
} from "../models/requests/tracking/CreateBusinessLocationRequest"

// =========================
// GET BUSINESS LOCATION
// =========================
export async function getBusinessLocation(
    id: string
): Promise<BusinessLocationResponse> {
    const res = await api.get<ApiResponse<BusinessLocationResponse>>(
        `/businesslocation/${id}`
    )

    return res.data.data!
}

// =========================
// GET BUSINESS LOCATIONS
// =========================
export async function getBusinessLocationsByBusinessId(
    businessId: string
): Promise<BusinessLocationResponse[]> {
    const res = await api.get<ApiResponse<BusinessLocationResponse[]>>(
        `/businesslocation/business/${businessId}`
    )

    return res.data.data!
}

// =========================
// CREATE BUSINESS LOCATION
// =========================
export async function createBusinessLocation(
    request: CreateBusinessLocationRequest
): Promise<BusinessLocationResponse> {
    const res = await api.post<ApiResponse<BusinessLocationResponse>>(
        "/businesslocation",
        request
    )

    return res.data.data!
}

// =========================
// DELETE BUSINESS LOCATION
// =========================
export async function deleteBusinessLocation(
    id: string
): Promise<boolean> {
    const res = await api.delete<ApiResponse<boolean>>(
        `/businesslocation/${id}`
    )

    return res.data.data!
}