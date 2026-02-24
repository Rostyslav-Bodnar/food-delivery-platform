import React from "react";
import "../styles/AuthForm.css";
import useLoginForm from "../hooks/useLoginForm";

const LoginForm = ({ onSwitch }) => {
    const {
        formData,
        error,
        handleChange,
        handleSubmit,
    } = useLoginForm();

    return (
        <>
            <h2>Welcome back</h2>

            {error && <p className="error-text">{error}</p>}

            <form className="auth-form" onSubmit={handleSubmit}>
                <input
                    name="email"
                    placeholder="Email"
                    onChange={handleChange}
                />
                <input
                    name="password"
                    type="password"
                    placeholder="Password"
                    onChange={handleChange}
                />
                <button type="submit">Login</button>
            </form>

            <p className="login-text">
                Don’t have an account?
                <button className="link-btn" onClick={onSwitch}>
                    Register
                </button>
            </p>
        </>
    );
};

export default LoginForm;