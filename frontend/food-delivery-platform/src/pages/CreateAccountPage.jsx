import React, { useState, useEffect } from "react";
import "./styles/CreateAccountPage.css";
import { Link } from "react-router-dom";
import CustomerForm from "../components/forms/CustomerForm";
import BusinessForm from "../components/forms/BusinessForm";
import CourierForm from "../components/forms/CourierForm";
import { getAccounts } from "../api/Account.jsx";
import { useUser } from "../context/UserContext";

const CreateAccountPage = () => {
    const { user, reloadUser } = useUser();
    const [accountType, setAccountType] = useState(null);
    const [existingAccounts, setExistingAccounts] = useState([]);

    const allAccountTypes = ["Customer", "Business", "Courier"];

    useEffect(() => {
        const fetchAccounts = async () => {
            if (!user) return;
            try {
                const accounts = await getAccounts(user.id);
                const existingTypes = accounts.map(acc => acc.accountType);
                setExistingAccounts(existingTypes);

                const availableType = allAccountTypes.find(
                    type => !existingTypes.includes(type)
                );
                setAccountType(availableType || null);

            } catch (err) {
                console.error("Failed to fetch accounts:", err);
            }
        };
        fetchAccounts();
    }, [user]);

    const renderForm = () => {
        switch (accountType) {
            case "Customer":
                return <CustomerForm />;
            case "Business":
                return <BusinessForm />;
            case "Courier":
                return <CourierForm />;
            default:
                return (
                    <p className="no-accounts-text">
                        You already have all available account types 🎉
                    </p>
                );
        }
    };

    const availableAccountTypes = allAccountTypes.filter(
        type => !existingAccounts.includes(type)
    );

    return (
        <div className="create-page-wrapper">
            <div className="create-container">
                <h2>Create Your Account</h2>
                {availableAccountTypes.length > 0 ? (
                    <>
                        <div className="tabs">
                            {availableAccountTypes.map(type => (
                                <button
                                    key={type}
                                    className={`tab ${accountType === type ? "active" : ""}`}
                                    onClick={() => setAccountType(type)}
                                >
                                    {type === "Customer" && "👤 Customer"}
                                    {type === "Business" && "🏪 Business"}
                                    {type === "Courier" && "🚚 Courier"}
                                </button>
                            ))}
                        </div>

                        <div className="form-wrapper fade-in">
                            {renderForm()}
                        </div>
                    </>
                ) : (
                    <p className="no-accounts-text">
                        You have already created all account types 🎉
                    </p>
                )}

                <p className="login-text">
                    Already have an account? <Link to="/login">Login</Link>
                </p>
            </div>
        </div>
    );
};

export default CreateAccountPage;
