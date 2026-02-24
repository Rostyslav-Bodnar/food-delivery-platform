import React from "react";
import { NavLink } from "react-router-dom";
import { useUser } from "../../context/UserContext";
import { AccountSection } from "./components/AccountSection";
import "./styles/HeaderComponent.css";

const Header = () => {
    const {
        user,
        accounts,
        currentAccountId,
        loading,
        logout,
        switchAccount
    } = useUser();

    if (loading) {
        return (
            <header className="header">
                <h1>Foodie Delivery</h1>
                <p>Loading...</p>
            </header>
        );
    }

    return (
        <header className="header">
            <NavLink className="page-header" to="/">
                Foodie Delivery
            </NavLink>

            <nav>
                <NavLink className="nav-link" to="/">
                    Home
                </NavLink>

                {!user && (
                    <>
                        <NavLink className="nav-link" to="/login">
                            Login
                        </NavLink>
                        <NavLink className="nav-link" to="/register">
                            Register
                        </NavLink>
                    </>
                )}

                {user && (
                    <AccountSection
                        user={user}
                        accounts={accounts}
                        currentAccountId={currentAccountId}
                        switchAccount={switchAccount}
                        logout={logout}
                    />
                )}
            </nav>
        </header>
    );
};

export default Header;