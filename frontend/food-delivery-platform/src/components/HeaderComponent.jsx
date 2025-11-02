// src/components/HeaderComponent.jsx
import React, { useState, useRef, useEffect } from "react";
import { NavLink, useNavigate } from "react-router-dom";
import { useUser } from "../context/UserContext";
import { motion, AnimatePresence } from "framer-motion";
import "./styles/HeaderComponent.css";

const Header = () => {
    const { user, accounts, currentAccountId, loading, logout, switchAccount } = useUser();
    const [isDropdownOpen, setIsDropdownOpen] = useState(false);
    const [isAccountsOpen, setIsAccountsOpen] = useState(true);
    const dropdownRef = useRef(null);
    const navigate = useNavigate();

    // Закриття дропдауна при кліку поза ним
    useEffect(() => {
        const handleOutsideClick = (event) => {
            if (dropdownRef.current && !dropdownRef.current.contains(event.target)) {
                setIsDropdownOpen(false);
                setIsAccountsOpen(true);
            }
        };
        if (isDropdownOpen) {
            document.addEventListener("mousedown", handleOutsideClick);
        }
        return () => {
            document.removeEventListener("mousedown", handleOutsideClick);
        };
    }, [isDropdownOpen]);

    // Переключення акаунта
    const handleSelectAccount = async (account) => {
        if (account.id === currentAccountId) {
            setIsDropdownOpen(false);
            return;
        }

        await switchAccount(account.id);
        // reload() вже в UserContext → не потрібно тут
        setIsDropdownOpen(false);
    };

    const toggleAccounts = () => setIsAccountsOpen(prev => !prev);

    if (loading) {
        return (
            <header className="header">
                <h1>Foodie Delivery</h1>
                <p>Loading...</p>
            </header>
        );
    }

    const activeAccount = accounts.find(acc => acc.id === currentAccountId) || {};
    const otherAccounts = accounts.filter(acc => acc.id !== currentAccountId);
    const totalAccounts = accounts.length;

    // Позиціонування інших акаунтів
    const getPosition = (index) => {
        if (totalAccounts === 1) return { x: 0, y: 0, zIndex: 3 };
        if (totalAccounts === 2) return { x: index === 0 ? -20 : 0, y: 0, zIndex: index === 0 ? 1 : 3 };
        if (index === 0) return { x: -20, y: 0, zIndex: 1 }; // Ліворуч
        if (index === 1) return { x: 20, y: 0, zIndex: 1 }; // Праворуч
        return { x: 0, y: 0, zIndex: 3 }; // Активний (центр)
    };

    return (
        <header className="header">
            <NavLink className="page-header" to="/">Foodie Delivery</NavLink>

            <nav>
                <NavLink className="nav-link" to="/">Home</NavLink>

                {!user && (
                    <>
                        <NavLink className="nav-link" to="/login">Login</NavLink>
                        <NavLink className="nav-link" to="/register">Register</NavLink>
                    </>
                )}

                {user && (
                    <div className="account-bubbles-container" ref={dropdownRef}>
                        <div className="account-bubbles">
                            {/* Активний акаунт */}
                            <motion.div
                                className="account-bubble active"
                                title={activeAccount.accountType}
                                onClick={() => setIsDropdownOpen(!isDropdownOpen)}
                                animate={{ x: 0, y: 0, scale: 1, zIndex: 3 }}
                                transition={{ type: "spring", stiffness: 200, damping: 20 }}
                            >
                                {activeAccount.imageUrl ? (
                                    <motion.img
                                        src={activeAccount.imageUrl}
                                        alt={activeAccount.accountType}
                                        whileHover={{ scale: 1.1 }}
                                        transition={{ duration: 0.2 }}
                                    />
                                ) : (
                                    <span>{activeAccount.name ? activeAccount.name[0].toUpperCase() : "U"}</span>
                                )}
                            </motion.div>

                            {/* Інші акаунти */}
                            {otherAccounts.map((acc, index) => {
                                const position = getPosition(index);
                                return (
                                    <motion.div
                                        key={acc.id}
                                        className="account-bubble"
                                        title={acc.accountType}
                                        onClick={() => setIsDropdownOpen(true)}
                                        animate={{
                                            x: position.x,
                                            y: position.y,
                                            scale: 1,
                                            zIndex: position.zIndex,
                                            opacity: 0.7,
                                        }}
                                        transition={{ type: "spring", stiffness: 200, damping: 20 }}
                                    >
                                        {acc.imageUrl ? (
                                            <motion.img
                                                src={acc.imageUrl}
                                                alt={acc.accountType}
                                                whileHover={{ scale: 1.1, opacity: 1 }}
                                                transition={{ duration: 0.2 }}
                                            />
                                        ) : (
                                            <span>{acc.name ? acc.name[0].toUpperCase() : "U"}</span>
                                        )}
                                    </motion.div>
                                );
                            })}
                        </div>

                        {/* Дропдаун */}
                        <AnimatePresence>
                            {isDropdownOpen && (
                                <motion.div
                                    className="dropdown-content-header show"
                                    initial={{ opacity: 0, y: -10 }}
                                    animate={{ opacity: 1, y: 0 }}
                                    exit={{ opacity: 0, y: -10 }}
                                    transition={{ duration: 0.3, ease: "easeOut" }}
                                >
                                    <h4>{user.name} {user.surname}</h4>

                                    <motion.button
                                        className="action-btn-header"
                                        whileHover={{ scale: 1.05 }}
                                        whileTap={{ scale: 0.95 }}
                                        onClick={() => navigate("/profile")}
                                    >
                                        Profile
                                    </motion.button>

                                    <h5 className="accounts-toggle" onClick={toggleAccounts}>
                                        Accounts {isAccountsOpen ? "▲" : "▼"}
                                    </h5>

                                    {isAccountsOpen && (
                                        <ul>
                                            {accounts.map((acc) => (
                                                <li
                                                    key={acc.id}
                                                    className={acc.id === currentAccountId ? "selected-account" : ""}
                                                    onClick={() => handleSelectAccount(acc)}
                                                >
                                                    {acc.accountType}
                                                </li>
                                            ))}
                                        </ul>
                                    )}

                                    {accounts.length < 3 && (
                                        <motion.button
                                            className="action-btn-header"
                                            whileHover={{ scale: 1.05 }}
                                            whileTap={{ scale: 0.95 }}
                                            onClick={() => navigate("/account/create")}
                                        >
                                            Create Account
                                        </motion.button>
                                    )}

                                    <motion.button
                                        className="action-btn-header"
                                        whileHover={{ scale: 1.05 }}
                                        whileTap={{ scale: 0.95 }}
                                        onClick={logout}
                                    >
                                        Logout
                                    </motion.button>
                                </motion.div>
                            )}
                        </AnimatePresence>
                    </div>
                )}
            </nav>
        </header>
    );
};

export default Header;