import React from 'react';
import { Link } from 'react-router-dom';
import './styles/HomePage.css';

const HomePage = () => {
    return (
        <div className="page-wrapper">
            <div className="hero-section">
                <h2 className="welcome">Welcome to Foodie Delivery 🍔</h2>
                <p className="hero-text">Craving something delicious? Order your favorite meals from top restaurants and have them delivered to your door in minutes!</p>
                <div className="cta-buttons">
                    <Link className="cta-button" to="/login">Login</Link>
                    <Link className="cta-button cta-button--register" to="/register">Register</Link>
                </div>
            </div>
        </div>
    );
};

export default HomePage;