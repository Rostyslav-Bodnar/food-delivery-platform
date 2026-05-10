import React from "react";
import AppRouter from "./AppRouter.jsx";
import { UserProvider, useUser } from "./context/UserContext.jsx";
import { ToastProvider, useToast } from "./global-components/toast/ToastContext.jsx";

function AppContent() {
    const { loading, error, clearError } = useUser();
    const { addToast } = useToast();

    React.useEffect(() => {
        if (!error) return;

        addToast({
            message: error
        });

        clearError();
    }, [error, addToast, clearError]);

    if (loading) {
        return <>Loading application…</>;
    }

    return <AppRouter />;
}

function App() {
    return (
        <UserProvider>
            <ToastProvider>
                <AppContent />
            </ToastProvider>
        </UserProvider>
    );
}

export default App;