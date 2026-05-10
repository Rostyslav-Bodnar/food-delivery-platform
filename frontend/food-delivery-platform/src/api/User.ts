// api/userApi.ts
import { api } from "./apiClient"
import type { ApiResponse } from "../models/responses/Response"
import type { UserDto } from "../models/responses/user/UserDto"

export async function getCurrentUser(): Promise<UserDto> {
    const res = await api.get<ApiResponse<UserDto>>("/user/profile")
    return res.data.data!
}

export async function getUser(userId: string): Promise<UserDto> {
    const res = await api.get<ApiResponse<UserDto>>(
        `/user/user?userId=${userId}`
    )
    return res.data.data!
}

export async function getUsers(): Promise<UserDto[]> {
    const res = await api.get<ApiResponse<UserDto[]>>("/user/users")
    return res.data.data!
}