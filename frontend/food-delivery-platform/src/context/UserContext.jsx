// src/context/UserContext.jsx
import React, { createContext, useContext, useState, useEffect } from "react";
import { getProfileData, switchAccount } from "../api/Profile";
import { logout } from "../api/Auth.ts";

const UserContext = createContext(null);

export const UserProvider = ({ children }) => {
    const [user, setUser] = useState(null);
    const [accounts, setAccounts] = useState([]);
    const [currentAccountId, setCurrentAccountId] = useState(null);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState(null);

    // =========================
    // LOAD PROFILE
    // =========================
    const loadUser = async () => {
        try {
            setLoading(true);
            setError(null);


            const token = localStorage.getItem("accessToken");
            if (!token) {
                setUser(null);
                setAccounts([]);
                setCurrentAccountId(null);
                return;
            }
            
            const data = await getProfileData();

            setUser(data.user);
            setAccounts(data.accounts);
            setCurrentAccountId(data.currentAccount.id);

            const currentAcc = data.accounts.find(
                a => a.id === data.currentAccount.id
            );

            if (currentAcc) {
                localStorage.setItem("currentAccountType", currentAcc.accountType);
                localStorage.setItem("currentAccountId", currentAcc.id);
                localStorage.setItem("currentAccountName", currentAcc.name ?? "");
            }
        } catch (err) {
            // ✅ новий стандарт
            setError(err.message);
            setUser(null);
        } finally {
            setLoading(false);
        }
    };

    useEffect(() => {
        loadUser();
    }, []);

    // =========================
    // SWITCH ACCOUNT
    // =========================
    const handleSwitchAccount = async (accountId) => {
        try {
            setError(null);

            await switchAccount(accountId);
            await loadUser();

            // UX‑рішення — залишаємо як було
            window.location.reload();
        } catch (err) {
            setError(err.message);
        }
    };

    // =========================
    // LOGOUT
    // =========================
    const handleLogout = async () => {
        try {
            await logout();
        } finally {
            localStorage.clear();
            window.location.href = "/food-delivery-platform/";
        }
    };

    return (
        <UserContext.Provider
            value={{
                user,
                accounts,
                currentAccountId,
                loading,
                error,
                reloadUser: loadUser,
                switchAccount: handleSwitchAccount,
                logout: handleLogout,
                clearError: () => setError(null)
            }}
        >
            {children}
        </UserContext.Provider>
    );
};

export const useUser = () => useContext(UserContext);