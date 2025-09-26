import React, { useState } from "react";
import "./RegisterForm.css";

const RegisterForm = () => {
    const [formData, setFormData] = useState({
        username: "",
        email: "",
        password: "",
    });

    const handleChange = (e) => {
        setFormData({ ...formData, [e.target.name]: e.target.value });
    };

    const handleSubmit = (e) => {
        e.preventDefault();
        alert(`Welcome, ${formData.username}! Your account has been created 🍕`);
    };

    return (
        <div className="page-wrapper">
            <div className="register-container">
                <h2>Create Your Account</h2>
                <form className="register-form" onSubmit={handleSubmit}>
                    <input
                        type="text"
                        name="username"
                        placeholder="Username"
                        value={formData.username}
                        onChange={handleChange}
                        required
                    />

                    <input
                        type="email"
                        name="email"
                        placeholder="Email address"
                        value={formData.email}
                        onChange={handleChange}
                        required
                    />

                    <input
                        type="password"
                        name="password"
                        placeholder="Password"
                        value={formData.password}
                        onChange={handleChange}
                        required
                    />

                    <button type="submit">Register</button>
                </form>
                <p className="login-text">
                    Already have an account? <a href="#">Login</a>
                </p>
            </div>
        </div>
    );
};

export default RegisterForm;
