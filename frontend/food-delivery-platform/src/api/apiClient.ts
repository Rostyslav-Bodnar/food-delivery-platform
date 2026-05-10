import axios, {
    AxiosError,
    AxiosInstance,
    AxiosResponse,
    InternalAxiosRequestConfig
} from "axios";

import type {
    ApiResponse
} from "../models/responses/Response";

import {
    showToast
} from "../global-components/toast/ToastService";

// =========================
// Axios config extension
// =========================

declare module "axios" {
    export interface InternalAxiosRequestConfig {
        skipErrorToast?: boolean;
    }
}

// =========================
// Axios instance
// =========================

export const api: AxiosInstance = axios.create({
    baseURL: "http://localhost:5229/api",
    withCredentials: true
});

// =========================
// Request interceptor
// =========================

api.interceptors.request.use(
    (
        config: InternalAxiosRequestConfig
    ) => {
        const token =
            localStorage.getItem("accessToken");

        if (token) {
            config.headers.Authorization =
                `Bearer ${token}`;
        }

        return config;
    }
);

// =========================
// Response interceptor
// =========================

api.interceptors.response.use(
    (
        response: AxiosResponse<
            ApiResponse<unknown>
        >
    ) => {
        const body = response.data;

        // Backend business error
        if (
            body &&
            body.success === false
        ) {
            const message =
                body.errorMassage ||
                "Something went wrong";

            if (
                !response.config.skipErrorToast
            ) {
                showToast({
                    message
                });
            }

            return Promise.reject(
                new Error(message)
            );
        }

        return response;
    },

    (
        error: AxiosError<
            ApiResponse<unknown>
        >
    ) => {
        const message =
            error.response?.data
                ?.errorMassage ||
            error.message ||
            "Network error";

        if (
            !error.config?.skipErrorToast
        ) {
            showToast({
                message
            });
        }

        return Promise.reject(
            new Error(message)
        );
    }
);