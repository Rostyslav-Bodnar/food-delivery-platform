import React from 'react';
import { Link, NavLink } from 'react-router-dom';
import './styles/HeaderComponent.css';
import {useState, useEffect, useRef } from "react";

const Header = () => {
    const user = {
        name: "Vladyslav Drohomeretskyi",
        accounts: ["Account 1", "Account 2", "Account 3"],
    };

    const [isDropdownOpen, setIsDropdownOpen] = useState(false);
    const [isAccountsOpen, setIsAccountsOpen] = useState(true);
    const dropdownRef = useRef(null);

    const handleToggleDropdown = () => {
        setIsDropdownOpen(!isDropdownOpen);
    };

    const handleToggleAccounts = () => {
        setIsAccountsOpen(!isAccountsOpen);
    };

    const handleClickOutside = (event) => {
        if (dropdownRef.current && !dropdownRef.current.contains(event.target)) {
            setIsDropdownOpen(false);
        }
    };

    useEffect(() => {
        document.addEventListener("mousedown", handleClickOutside);
        return () => {
            document.removeEventListener("mousedown", handleClickOutside);
        };
    }, []);

    return (
        <header className="header">
            <h1>Foodie Delivery 🍔</h1>
            <nav>
                <NavLink className="nav-link" to="/">Home</NavLink>
                <NavLink className="nav-link" to="/login">Login</NavLink>
                <NavLink className="nav-link" to="/register">Register</NavLink>

                {/* Dropdown акаунтів */}
                <div className="dropdown-header" ref={dropdownRef}>
                    <button className="dropbtn-header" onClick={handleToggleDropdown}>
                        {user.name[0].toUpperCase()}
                    </button>
                    <div className={`dropdown-content-header ${isDropdownOpen ? "show" : ""}`}>
                        <h4>{user.name}</h4>
                        <button className="action-btn-header">
                            Profile
                        </button>
                        <h5 className="accounts-toggle" onClick={handleToggleAccounts}>
                            Accounts {isAccountsOpen ? "▲" : "▼"}
                        </h5>
                        {isAccountsOpen && (
                            <ul>
                                {user.accounts.map((acc, idx) => (
                                    <li key={idx}>
                                         {acc}
                                    </li>
                                ))}
                            </ul>
                        )}
                        <button className="action-btn-header">
                             Create Account
                        </button>
                        <button className="action-btn-header">
                             Logout
                        </button>
                    </div>
                </div>
            </nav>
        </header>
    );
};

export default Header;