// api/authApi.ts
import { api } from "./apiClient"
import type { ApiResponse } from "../models/responses/Response"
import type { TokenResponse } from "../models/responses/auth/TokenResponse"
import type { LoginRequest } from "../models/requests/auth/LoginRequest"
import type { RegisterRequest } from "../models/requests/auth/RegisterRequest"

// =========================
// LOGIN
// =========================
export async function login(
    credentials: LoginRequest
): Promise<TokenResponse> {
    const res = await api.post<ApiResponse<TokenResponse>>(
        "/auth/login",
        credentials
    )

    saveTokens(res.data.data!)
    return res.data.data!
}

// =========================
// REGISTER
// =========================
export async function register(
    data: RegisterRequest
): Promise<TokenResponse> {
    const res = await api.post<ApiResponse<TokenResponse>>(
        "/auth/register",
        data
    )

    saveTokens(res.data.data!)
    return res.data.data!
}

// =========================
// REFRESH
// =========================
export async function refresh(): Promise<TokenResponse> {
    const res = await api.post<ApiResponse<TokenResponse>>(
        "/auth/refresh"
    )

    saveTokens(res.data.data!)
    return res.data.data!
}

// =========================
// LOGOUT
// =========================
export async function logout(): Promise<void> {
    await api.post<ApiResponse<boolean>>("/auth/logout")
    clearTokens()
}

// =========================
// TOKEN STORAGE
// =========================
export function saveTokens(tokens: TokenResponse): void {
    localStorage.setItem("accessToken", tokens.accessToken)
    localStorage.setItem(
        "accessTokenExpiresAt",
        tokens.accessTokenExpiresAt
    )
}

function clearTokens(): void {
    localStorage.removeItem("accessToken")
    localStorage.removeItem("accessTokenExpiresAt")
}