import React from 'react';
import { Link, NavLink } from 'react-router-dom';
import './styles/HeaderComponent.css';

const Header = () => {
    return (
        <header className="header">
            <h1>Foodie Delivery 🍔</h1>
            <nav>
                <NavLink className="nav-link" to="/" activeClassName="active">Home</NavLink>
                <NavLink className="nav-link" to="/login" activeClassName="active">Login</NavLink>
                <NavLink className="nav-link" to="/register" activeClassName="active">Register</NavLink>
            </nav>
        </header>
    );
};

export default Header;