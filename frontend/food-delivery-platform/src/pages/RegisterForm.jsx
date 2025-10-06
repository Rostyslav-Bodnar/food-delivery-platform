import React, { useState } from 'react';
import { Link } from 'react-router-dom';
import { register } from '../api/auth';
import './styles/RegisterForm.css';

const RegisterForm = () => {
    const [formData, setFormData] = useState({
        name: '',
        surname: '',
        email: '',
        password: '',
    });
    const [error, setError] = useState(null);

    const handleChange = (e) => {
        setFormData({ ...formData, [e.target.name]: e.target.value });
    };

    const handleSubmit = async (e) => {
        e.preventDefault();
        setError(null);
        try {
            const response = await register(formData);
            alert(`Welcome, ${formData.name}! 🎉`);

            // Перенаправляємо користувача після реєстрації
            window.location.href = "/food-delivery-platform/profile";
        } catch (err) {
            setError(err.response?.data || 'Failed to register. Please try again.');
        }
    };

    return (
        <div className="page-wrapper">
            <div className="register-container">
                <h2>Create Your Account</h2>
                {error && <p className="error-text">{error}</p>}
                <form className="register-form" onSubmit={handleSubmit}>
                    <div className="fullname-inputs">
                        <input
                            type="text"
                            name="name"
                            placeholder="First Name"
                            value={formData.name}
                            onChange={handleChange}
                            required
                        />
                        <input
                            type="text"
                            name="surname"
                            placeholder="Last Name"
                            value={formData.surname}
                            onChange={handleChange}
                            required
                        />
                    </div>
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
                    Already have an account? <Link to="/login">Login</Link>
                </p>
            </div>
        </div>
    );
};

export default RegisterForm;
