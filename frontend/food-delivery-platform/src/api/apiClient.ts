import axios, {
    AxiosError,
    AxiosInstance,
    AxiosResponse
} from "axios"
import type { ApiResponse } from "../models/responses/Response"

// =========================
// Axios instance
// =========================
export const api: AxiosInstance = axios.create({
    baseURL: "http://localhost:5229/api", // Gateway base URL
    withCredentials: true
})

// =========================
// Request interceptor (JWT)
// =========================
api.interceptors.request.use(config => {
    const token = localStorage.getItem("accessToken")
    if (token) {
        config.headers.Authorization = `Bearer ${token}`
    }
    return config
})

// =========================
// Response interceptor (ERROR HANDLING)
// =========================
api.interceptors.response.use(
    (response: AxiosResponse<ApiResponse<unknown>>) => {
        const body = response.data

        // ✅ Backend‑controlled business error
        if (body && body.success === false) {
            throw new Error(body.errorMassage ?? "Unknown error")
        }

        return response
    },
    (error: AxiosError<ApiResponse<unknown>>) => {
        // ✅ Gateway error response
        const message =
            error.response?.data?.errorMassage ??
            error.message ??
            "Network error"

        return Promise.reject(new Error(message))
    }
)