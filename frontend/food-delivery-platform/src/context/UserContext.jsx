// src/context/UserContext.jsx
import React, { createContext, useContext, useState, useEffect } from "react";
import { getProfileData, switchAccount } from "../api/Profile.jsx";
import { refresh, logout } from "../api/Auth.jsx";

const UserContext = createContext();

export const UserProvider = ({ children }) => {
    const [user, setUser] = useState(null);
    const [accounts, setAccounts] = useState([]);
    const [currentAccountId, setCurrentAccountId] = useState(null);
    const [loading, setLoading] = useState(true);

    const loadUser = async () => {
        try {
            let token = localStorage.getItem("accessToken");
            if (!token) {
                const tokens = await refresh();
                token = tokens.accessToken;
            }
            const data = await getProfileData(token);
            setUser(data.user);
            setAccounts(data.accounts);
            setCurrentAccountId(data.currentAccount.id);

            // Зберігаємо accountType у localStorage
            const currentAcc = data.accounts.find(a => a.id === data.currentAccount.id);
            console.log(currentAcc);

            if (currentAcc) {
                localStorage.setItem("currentAccountType", currentAcc.accountType);
                localStorage.setItem("currentAccountId", data.currentAccount.id);
            }
        } catch (err) {
            console.error("Load user error:", err);
            setUser(null);
        } finally {
            setLoading(false);
        }
    };

    useEffect(() => {
        loadUser();
    }, []);

    const handleSwitchAccount = async (accountId) => {
        try {
            let token = localStorage.getItem("accessToken");
            if (!token) {
                const tokens = await refresh();
                token = tokens.accessToken;
            }

            await switchAccount(accountId, token);
            await loadUser(); // оновлюємо дані

            // ПЕРЕЗАВАНТАЖУЄМО СТОРІНКУ ПІСЛЯ ПЕРЕМИКАННЯ
            window.location.reload();
        } catch (err) {
            console.error("Switch account error:", err);
        }
    };

    const handleLogout = async () => {
        try {
            await logout();
        } catch (err) {
            console.warn("Logout error:", err);
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
                reloadUser: loadUser,
                switchAccount: handleSwitchAccount,
                logout: handleLogout,
            }}
        >
            {children}
        </UserContext.Provider>
    );
};

export const useUser = () => useContext(UserContext);