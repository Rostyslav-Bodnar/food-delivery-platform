import React, { useState } from "react";
import { register } from "../../api/auth";
import "./styles/AuthForm.css";

const RegisterForm = ({ onSwitch }) => {
    const [formData, setFormData] = useState({
        name: "", surname: "", email: "", password: ""
    });
    const [error, setError] = useState(null);

    const handleChange = (e) =>
        setFormData({ ...formData, [e.target.name]: e.target.value });

    const handleSubmit = async (e) => {
        e.preventDefault();
        setError(null);
        try {
            await register(formData);
            window.location.href = "/food-delivery-platform/profile";
        } catch {
            setError("Registration failed");
        }
    };

    return (
        <>
            <h2>Create account</h2>

            {error && <p className="error-text">{error}</p>}

            <form className="auth-form" onSubmit={handleSubmit}>
                <div className="fullname-inputs">
                    <input name="name" placeholder="First name" onChange={handleChange} />
                    <input name="surname" placeholder="Last name" onChange={handleChange} />
                </div>
                <input name="email" placeholder="Email" onChange={handleChange} />
                <input name="password" type="password" placeholder="Password" onChange={handleChange} />
                <button type="submit">Register</button>
            </form>

            <p className="login-text">
                Already have an account?
                <button className="link-btn" onClick={onSwitch}>Login</button>
            </p>
        </>
    );
};

export default RegisterForm;
