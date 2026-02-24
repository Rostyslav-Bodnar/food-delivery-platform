import { motion, AnimatePresence } from "framer-motion";
import {useState} from "react";

export function AccountDropdown({
                                    isOpen,
                                    user,
                                    accounts,
                                    currentAccountId,
                                    navigate,
                                    handleSelectAccount,
                                    logout
                                }) {
    const [isAccountsOpen, setIsAccountsOpen] = useState(true);

    return (
        <AnimatePresence>
            {isOpen && (
                <motion.div
                    className="dropdown-content-header show"
                    initial={{ opacity: 0, y: -10 }}
                    animate={{ opacity: 1, y: 0 }}
                    exit={{ opacity: 0, y: -10 }}
                >
                    <h4>{user.name} {user.surname}</h4>

                    <button
                        className="action-btn-header"
                        onClick={() => navigate("/profile")}
                    >
                        Profile
                    </button>

                    <h5
                        className="accounts-toggle"
                        onClick={() =>
                            setIsAccountsOpen(prev => !prev)
                        }
                    >
                        Accounts {isAccountsOpen ? "▲" : "▼"}
                    </h5>

                    {isAccountsOpen && (
                        <ul>
                            {accounts.map(acc => (
                                <li
                                    key={acc.id}
                                    className={
                                        acc.id === currentAccountId
                                            ? "selected-account"
                                            : ""
                                    }
                                    onClick={() =>
                                        handleSelectAccount(acc)
                                    }
                                >
                                    {acc.accountType}
                                </li>
                            ))}
                        </ul>
                    )}

                    {accounts.length < 3 && (
                        <button
                            className="action-btn-header"
                            onClick={() =>
                                navigate("/account/create")
                            }
                        >
                            Create Account
                        </button>
                    )}

                    <button
                        className="action-btn-header"
                        onClick={logout}
                    >
                        Logout
                    </button>
                </motion.div>
            )}
        </AnimatePresence>
    );
}