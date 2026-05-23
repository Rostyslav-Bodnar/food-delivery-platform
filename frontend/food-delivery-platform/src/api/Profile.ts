// api/profileApi.ts
import { api } from "./apiClient"
import type { ApiResponse } from "../models/responses/Response"
import type { ProfileResponse } from "../models/responses/profile/ProfileResponse"
import { saveTokens } from "./Auth"
import {TokenResponse} from "../models/responses/auth/TokenResponse";

export async function getProfileData(): Promise<ProfileResponse> {
    const res = await api.get<ApiResponse<ProfileResponse>>("/profile")
    return res.data.data!
}

export async function switchAccount(accountId: string): Promise<void> {
    const res = await api.put<ApiResponse<TokenResponse>>(
        `/profile/switch/${accountId}`
    )

    saveTokens(res.data.data!)
}