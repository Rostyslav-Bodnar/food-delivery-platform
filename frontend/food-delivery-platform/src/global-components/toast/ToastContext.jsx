import {
    createContext,
    useContext,
    useState,
    useCallback,
    useEffect
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

    useEffect(() => {
        registerToastHandler(addToast);

        return () => {
            unregisterToastHandler();
        };
    }, [addToast]);

    return (
        <ToastContext.Provider
            value={{
                addToast,
                removeToast
            }}
        >
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