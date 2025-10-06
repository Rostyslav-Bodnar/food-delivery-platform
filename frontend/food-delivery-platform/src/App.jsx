import React from "react";
import AppRouter from "./AppRouter.jsx";
import { UserProvider } from "./context/UserContext.jsx";

function App() {
    return (
        <UserProvider>
            <AppRouter />
        </UserProvider>
    );
}

export default App;
