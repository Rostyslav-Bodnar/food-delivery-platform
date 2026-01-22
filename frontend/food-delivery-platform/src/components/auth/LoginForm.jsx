import React, { useState } from "react";
import { login } from "../../api/auth";
import "./styles/AuthForm.css";

const LoginForm = ({ onSwitch }) => {
    const [formData, setFormData] = useState({ email: "", password: "" });
    const [error, setError] = useState(null);

    const handleChange = (e) =>
        setFormData({ ...formData, [e.target.name]: e.target.value });

    const handleSubmit = async (e) => {
        e.preventDefault();
        setError(null);
        try {
            await login(formData);
            window.location.href = "/food-delivery-platform/profile";
        } catch {
            setError("Invalid email or password");
        }
    };

    return (
        <>
            <h2>Welcome back</h2>

            {error && <p className="error-text">{error}</p>}

            <form className="auth-form" onSubmit={handleSubmit}>
                <input name="email" placeholder="Email" onChange={handleChange} />
                <input name="password" type="password" placeholder="Password" onChange={handleChange} />
                <button type="submit">Login</button>
            </form>

            <p className="login-text">
                Don’t have an account?
                <button className="link-btn" onClick={onSwitch}>Register</button>
            </p>
        </>
    );
};

export default LoginForm;
