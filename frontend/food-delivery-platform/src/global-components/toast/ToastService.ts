export type ToastPayload = {
    message: string;
    type?: string;
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