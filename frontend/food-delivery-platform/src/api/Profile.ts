// api/profileApi.ts
import { api } from "./apiClient"
import type { ApiResponse } from "../models/responses/Response"
import type { ProfileResponse } from "../models/responses/profile/ProfileResponse"

export async function getProfileData(): Promise<ProfileResponse> {
    const res = await api.get<ApiResponse<ProfileResponse>>("/profile")
    return res.data.data!
}

export async function switchAccount(accountId: string): Promise<void> {
    await api.put<ApiResponse<object>>(`/profile/switch/${accountId}`)
}