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

// Errors thrown by the response interceptor carry this flag once their
// message has already been shown to the user via showToast, so consumer
// catch blocks can avoid double-toasting the same failure.
export type ToastShownError = Error & { toastShown?: boolean };

const wrapError = (message: string): ToastShownError => {
    const err: ToastShownError = new Error(message);
    err.toastShown = true;
    return err;
};

// =========================
// Axios instance
// =========================

// Gateway base URL. Defaults to the local dev gateway so `npm run dev`
// works without any env file; production (Vercel) overrides via the
// `VITE_GATEWAY_API_URL` env var defined in the project settings.
const trimTrailingSlash = (value: string) => value.replace(/\/+$/, "");

const gatewayBaseUrl = trimTrailingSlash(
    import.meta.env.VITE_GATEWAY_API_URL ?? "http://localhost:5229/api"
);

export const api: AxiosInstance = axios.create({
    baseURL: gatewayBaseUrl,
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

            const skip = response.config.skipErrorToast;

            if (!skip) {
                showToast({
                    message,
                    type: "error"
                });
            }

            return Promise.reject(
                skip ? new Error(message) : wrapError(message)
            );
        }

        return response;
    },

    (
        error: AxiosError<
            ApiResponse<unknown>
        >
    ) => {
        // Suppress toast/error for canceled or aborted requests. These are
        // typically caused by React StrictMode double-mount in dev or
        // component unmount during navigation — not real failures.
        // axios.isCancel covers CancelToken / AbortController paths;
        // ECONNABORTED with message "Request aborted" covers the
        // browser-level xhr.onabort case (e.g. user navigates away).
        const isAbort =
            axios.isCancel(error) ||
            (error.code === "ECONNABORTED" &&
                error.message === "Request aborted");

        if (isAbort) {
            return Promise.reject(error);
        }

        const message =
            error.response?.data
                ?.errorMassage ||
            error.message ||
            "Network error";

        const skip = error.config?.skipErrorToast;

        if (!skip) {
            showToast({
                message,
                type: "error"
            });
        }

        return Promise.reject(
            skip ? new Error(message) : wrapError(message)
        );
    }
);