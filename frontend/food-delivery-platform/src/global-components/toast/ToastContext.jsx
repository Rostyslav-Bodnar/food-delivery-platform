import {
    createContext,
    useContext,
    useState,
    useCallback,
    useEffect,
    useMemo
} from "react";

import ToastContainer from "./ToastContainer";

import {
    registerToastHandler,
    unregisterToastHandler
} from "./ToastService";

const ToastContext = createContext(null);

export function ToastProvider({ children }) {
    const [toasts, setToasts] = useState([]);

    const removeToast = useCallback((id) => {
        setToasts(prev =>
            prev.filter(t => t.id !== id)
        );
    }, []);

    const addToast = useCallback((toast) => {
        const id = crypto.randomUUID();

        setToasts(prev => [
            ...prev,
            {
                id,
                type: "error",
                autoHideMs: 4000,
                ...toast
            }
        ]);

        return id;
    }, []);

    const success = useCallback(
        (message, options) => addToast({ message, ...options, type: "success" }),
        [addToast]
    );

    const error = useCallback(
        (message, options) => addToast({ message, ...options, type: "error" }),
        [addToast]
    );

    const info = useCallback(
        (message, options) => addToast({ message, ...options, type: "info" }),
        [addToast]
    );

    const warning = useCallback(
        (message, options) => addToast({ message, ...options, type: "warning" }),
        [addToast]
    );

    useEffect(() => {
        registerToastHandler(addToast);

        return () => {
            unregisterToastHandler();
        };
    }, [addToast]);

    const value = useMemo(
        () => ({
            addToast,
            removeToast,
            success,
            error,
            info,
            warning
        }),
        [addToast, removeToast, success, error, info, warning]
    );

    return (
        <ToastContext.Provider value={value}>
            {children}

            <ToastContainer
                toasts={toasts}
                removeToast={removeToast}
            />
        </ToastContext.Provider>
    );
}

export function useToast() {
    const ctx = useContext(ToastContext);

    if (!ctx) {
        throw new Error(
            "useToast must be used inside ToastProvider"
        );
    }

    return ctx;
}
