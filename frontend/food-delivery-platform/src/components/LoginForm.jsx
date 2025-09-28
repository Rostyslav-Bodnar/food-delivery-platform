import React, { useState } from 'react';
import { Link } from 'react-router-dom';
import { login } from '../api/auth';
import './styles/LoginForm.css';

const LoginForm = () => {
    const [formData, setFormData] = useState({
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
            const response = await login(formData);
            localStorage.setItem('token', response.token);
            alert(`Welcome back, ${formData.email}! 🍔`);
            setFormData({ email: '', password: '' });
        } catch (err) {
            setError(err.message || 'Failed to login. Please check your credentials.');
        }
    };

    return (
        <div className="page-wrapper">
            <div className="register-container">
                <h2>Login to Foodie Delivery 🍔</h2>
                {error && <p className="error-text">{error}</p>}
                <form className="register-form" onSubmit={handleSubmit}>
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
                    <button type="submit">Login</button>
                </form>
                <p className="login-text">
                    Don't have an account? <Link to="/register">Register</Link>
                </p>
            </div>
        </div>
    );
};

export default LoginForm;