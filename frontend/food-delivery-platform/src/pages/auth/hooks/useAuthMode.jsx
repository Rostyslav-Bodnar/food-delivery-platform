import { useState } from "react";

const useAuthMode = () => {
    const [mode, setMode] = useState("login");

    const switchToLogin = () => setMode("login");
    const switchToRegister = () => setMode("register");

    return {
        mode,
        switchToLogin,
        switchToRegister,
    };
};

export default useAuthMode;