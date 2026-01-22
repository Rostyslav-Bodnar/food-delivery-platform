import React, { useState } from "react";
import LoginForm from "../components/auth/LoginForm";
import RegisterForm from "../components/auth/RegisterForm";
import "./styles/AuthorizationPage.css";

const AuthorizationPage = () => {
    const [mode, setMode] = useState("login");

    return (
        <div className="auth-page">
            <div className="auth-card">
                {mode === "login" ? (
                    <LoginForm onSwitch={() => setMode("register")} />
                ) : (
                    <RegisterForm onSwitch={() => setMode("login")} />
                )}
            </div>
        </div>
    );
};

export default AuthorizationPage;
