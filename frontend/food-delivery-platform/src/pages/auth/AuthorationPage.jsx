import React from "react";
import LoginForm from "./components/LoginForm";
import RegisterForm from "./components/RegisterForm";
import useAuthMode from "./hooks/useAuthMode";
import "./styles/AuthorizationPage.css";

const AuthorizationPage = () => {
    const { mode, switchToLogin, switchToRegister } = useAuthMode();

    return (
        <div className="auth-page">
            <div className="auth-card">
                {mode === "login" ? (
                    <LoginForm onSwitch={switchToRegister} />
                ) : (
                    <RegisterForm onSwitch={switchToLogin} />
                )}
            </div>
        </div>
    );
};

export default AuthorizationPage;