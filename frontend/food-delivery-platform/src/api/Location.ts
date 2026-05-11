import { api } from "./apiClient"

import type { ApiResponse } from "../models/responses/Response"

import type {
    LocationResponse
} from "../models/responses/tracking/LocationResponse"

import type {
    CreateLocationRequest,
    UpdateLocationRequest,
    AddLocationRequest
} from "../models/requests/tracking/LocationRequest"

// =========================
// GET LOCATION
// =========================
export async function getLocation(
    id: string
): Promise<LocationResponse> {
    const res = await api.get<ApiResponse<LocationResponse>>(
        `/location/${id}`
    )

    return res.data.data!
}

// =========================
// GET ALL LOCATIONS
// =========================
export async function getLocations(): Promise<LocationResponse[]> {
    const res = await api.get<ApiResponse<LocationResponse[]>>(
        "/location"
    )

    return res.data.data!
}

// =========================
// CREATE LOCATION
// =========================
export async function createLocation(
    request: CreateLocationRequest
): Promise<LocationResponse> {
    const res = await api.post<ApiResponse<LocationResponse>>(
        "/location",
        request
    )

    return res.data.data!
}

// =========================
// UPDATE LOCATION
// =========================
export async function updateLocation(
    request: UpdateLocationRequest
): Promise<LocationResponse> {
    const res = await api.put<ApiResponse<LocationResponse>>(
        "/location",
        request
    )

    return res.data.data!
}

// =========================
// DELETE LOCATION
// =========================
export async function deleteLocation(
    id: string
): Promise<boolean> {
    const res = await api.delete<ApiResponse<boolean>>(
        `/location/${id}`
    )

    return res.data.data!
}

// =========================
// ADD BUSINESS LOCATION
// =========================
export async function addBusinessLocation(
    request: AddLocationRequest
): Promise<LocationResponse> {
    const res = await api.post<ApiResponse<LocationResponse>>(
        "/location/add",
        request
    )

    return res.data.data!
}