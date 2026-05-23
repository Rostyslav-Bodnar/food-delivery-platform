export type ToastType = "success" | "error" | "info" | "warning";

export type ToastPayload = {
    message: string;
    type?: ToastType;
    title?: string | null;
    autoHideMs?: number;
};

type ToastHandler = (
    toast: ToastPayload
) => void;

let toastHandler: ToastHandler | null = null;

export const registerToastHandler = (
    handler: ToastHandler
): void => {
    toastHandler = handler;
};

export const unregisterToastHandler =
    (): void => {
        toastHandler = null;
    };

export const showToast = (
    toast: ToastPayload
): void => {
    if (!toastHandler) {
        return;
    }

    toastHandler(toast);
};

export const showSuccessToast = (
    message: string,
    options?: Omit<ToastPayload, "message" | "type">
): void => showToast({ message, type: "success", ...options });

export const showErrorToast = (
    message: string,
    options?: Omit<ToastPayload, "message" | "type">
): void => showToast({ message, type: "error", ...options });

export const showInfoToast = (
    message: string,
    options?: Omit<ToastPayload, "message" | "type">
): void => showToast({ message, type: "info", ...options });

export const showWarningToast = (
    message: string,
    options?: Omit<ToastPayload, "message" | "type">
): void => showToast({ message, type: "warning", ...options });
