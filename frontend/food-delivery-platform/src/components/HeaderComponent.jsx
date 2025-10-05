import React, { useState, useEffect, useRef } from "react";
import { NavLink, useNavigate } from "react-router-dom";
import "./styles/HeaderComponent.css";
import { getProfile } from "../api/User.jsx";
import { getAccounts } from "../api/Account.jsx";
import { refresh, logout } from "../api/Auth.jsx";

const Header = () => {
    const [user, setUser] = useState(null);
    const [accounts, setAccounts] = useState([]);
    const [selectedAccount, setSelectedAccount] = useState(null);
    const [isDropdownOpen, setIsDropdownOpen] = useState(false);
    const [isAccountsOpen, setIsAccountsOpen] = useState(false);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState(null);

    const dropdownRef = useRef(null);
    const navigate = useNavigate();

    // --- Fetch user data ---
    const refreshCalled = useRef(false);

    useEffect(() => {
        if (refreshCalled.current) return;

        const fetchData = async () => {
            try {
                await refresh();
                const profile = await getProfile();
                setUser(profile);
                // Fetch accounts from API
                const userAccounts = await getAccounts(profile.id);
                setAccounts(userAccounts);
                if (userAccounts.length > 0) {
                    setSelectedAccount(userAccounts[0].accountType);
                }
            } catch (err) {
                console.error("Failed to load user or accounts:", err);
                setError(err.response?.data || err.message || "Failed to load user data");
            } finally {
                setLoading(false);
            }
        };

        fetchData();
        refreshCalled.current = true;
    }, []);

    // --- Close dropdown on outside click ---
    useEffect(() => {
        const handleClickOutside = (event) => {
            if (dropdownRef.current && !dropdownRef.current.contains(event.target)) {
                setIsDropdownOpen(false);
            }
        };
        document.addEventListener("mousedown", handleClickOutside);
        return () => document.removeEventListener("mousedown", handleClickOutside);
    }, []);

    const handleToggleDropdown = () => setIsDropdownOpen(!isDropdownOpen);
    const handleToggleAccounts = () => setIsAccountsOpen(!isAccountsOpen);

    const handleSelectAccount = (accountType) => {
        setSelectedAccount(accountType);
    };

    const handleLogout = async () => {
        await logout();
        navigate("/login");
    };

    if (loading) {
        return (
            <header className="header">
                <h1>Foodie Delivery 🍔</h1>
                <p>Loading...</p>
            </header>
        );
    }

    if (error) {
        return (
            <header className="header">
                <h1>Foodie Delivery 🍔</h1>
                <p className="error-text">{error}</p>
            </header>
        );
    }

    return (
        <header className="header">
            <h1>Foodie Delivery 🍔</h1>
            <nav>
                <NavLink className="nav-link" to="/">Home</NavLink>
                {!user && (
                    <>
                        <NavLink className="nav-link" to="/login">Login</NavLink>
                        <NavLink className="nav-link" to="/register">Register</NavLink>
                    </>
                )}

                {user && (
                    <div className="dropdown-header" ref={dropdownRef}>
                        <button className="dropbtn-header" onClick={handleToggleDropdown}>
                            {user.fullName ? user.fullName[0].toUpperCase() : "U"}
                        </button>

                        <div className={`dropdown-content-header ${isDropdownOpen ? "show" : ""}`}>
                            <h4>{user.fullName}</h4>

                            <button className="action-btn-header" onClick={() => navigate("/profile")}>
                                Profile
                            </button>

                            <h5 className="accounts-toggle" onClick={handleToggleAccounts}>
                                Accounts {isAccountsOpen ? "▲" : "▼"}
                            </h5>

                            {isAccountsOpen && accounts.length > 0 ? (
                                <ul>
                                    {accounts.map((acc, idx) => (
                                        <li
                                            key={idx}
                                            className={selectedAccount === acc.accountType ? "selected-account" : ""}
                                            onClick={() => handleSelectAccount(acc.accountType)}
                                        >
                                            {acc.accountType}
                                        </li>
                                    ))}
                                </ul>
                            ) : (
                                isAccountsOpen && <p>No accounts yet</p>
                            )}

                            <button className="action-btn-header" onClick={() => navigate("/create-account")}>
                                Create Account
                            </button>

                            <button className="action-btn-header" onClick={handleLogout}>
                                Logout
                            </button>
                        </div>
                    </div>
                )}
            </nav>
        </header>
    );
};

export default Header;