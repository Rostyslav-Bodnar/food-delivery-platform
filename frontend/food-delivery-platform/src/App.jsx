import React from "react";

import AppRouter from "./AppRouter.jsx";

import {
    UserProvider
} from "./context/UserContext.jsx";

import {
    ToastProvider
} from "./global-components/toast/ToastContext.jsx";

function AppContent() {
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