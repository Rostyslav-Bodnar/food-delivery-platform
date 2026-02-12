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
                <aside className="create-sidebar">
                    <div className="create-brand">
                        <div className="create-logo">FE</div>
                        <div>
                            <div className="create-title">Create account</div>
                            <div className="create-sub">Select account type and fill the form</div>
                        </div>
                    </div>

                    <div className="tabs" role="tablist" aria-label="Account types">
                        {availableAccountTypes.map(type => (
                            <button
                                key={type}
                                role="tab"
                                aria-selected={accountType === type}
                                className={`tab ${accountType === type ? "active" : ""}`}
                                onClick={() => setAccountType(type)}
                            >
                                {type === "Customer" && "👤 Customer"}
                                {type === "Business" && "🏪 Business"}
                                {type === "Courier" && "🚚 Courier"}
                            </button>
                        ))}
                    </div>

                    <div className="create-footer">
                        <div>Need help? Contact support.</div>
                    </div>
                </aside>

                <section className="form-area">
                    <h2>{accountType ? `${accountType} — registration` : "Select account type"}</h2>
                    <div className="form-wrapper fade-in">
                        {renderForm()}
                    </div>
                </section>
            </div>
        </div>
    );

};

export default CreateAccountPage;
