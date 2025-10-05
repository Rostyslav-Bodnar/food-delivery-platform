import React, { useState } from "react";
import "./styles/CreateAccountPage.css";
import { Link } from "react-router-dom";
import CustomerForm from "../components/forms/CustomerForm";
import BusinessForm from "../components/forms/BusinessForm";
import CourierForm from "../components/forms/CourierForm";

const CreateAccountPage = () => {
    const [accountType, setAccountType] = useState("Customer");

    const renderForm = () => {
        switch (accountType) {
            case "Customer":
                return <CustomerForm />;
            case "Business":
                return <BusinessForm />;
            case "Courier":
                return <CourierForm />;
            default:
                return null;
        }
    };

    return (
        <div className="create-page-wrapper">
            <div className="create-container">
                <h2>Create Your Account</h2>
                <div className="tabs">
                    <button
                        className={`tab ${accountType === "Customer" ? "active" : ""}`}
                        onClick={() => setAccountType("Customer")}
                    >
                        👤 Customer
                    </button>
                    <button
                        className={`tab ${accountType === "Business" ? "active" : ""}`}
                        onClick={() => setAccountType("Business")}
                    >
                        🏪 Business
                    </button>
                    <button
                        className={`tab ${accountType === "Courier" ? "active" : ""}`}
                        onClick={() => setAccountType("Courier")}
                    >
                        🚚 Courier
                    </button>
                </div>
                <div className="form-wrapper fade-in">
                    {renderForm()}
                </div>
                <p className="login-text">
                    Already have an account? <Link to="/login">Login</Link>
                </p>
            </div>
        </div>
    );
};

export default CreateAccountPage;