import React from "react";
import "../styles/AuthForm.css";
import useRegisterForm from "../hooks/useRegisterForm";

const RegisterForm = ({ onSwitch }) => {
    const {
        formData,
        error,
        handleChange,
        handleSubmit,
    } = useRegisterForm();

    return (
        <>
            <h2>Create account</h2>

            {error && <p className="error-text">{error}</p>}

            <form className="auth-form" onSubmit={handleSubmit}>
                <div className="fullname-inputs">
                    <input
                        name="name"
                        placeholder="First name"
                        onChange={handleChange}
                    />
                    <input
                        name="surname"
                        placeholder="Last name"
                        onChange={handleChange}
                    />
                </div>

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

                <button type="submit">Register</button>
            </form>

            <p className="login-text">
                Already have an account?
                <button className="link-btn" onClick={onSwitch}>
                    Login
                </button>
            </p>
        </>
    );
};

export default RegisterForm;