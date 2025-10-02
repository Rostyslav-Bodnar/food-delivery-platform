import React, { useState } from "react";
import "./styles/AccountPage.css";

const AccountPage = () => {
    const [user] = useState({
        fullName: "Vladyslav Drohomereckyy",
        email: "vlad@example.com",
        accounts: ["Personal Account", "Work Account", "Test Account"],
    });

    const handleLogout = () => {
        alert("You have logged out!");
        window.location.href = "/food-delivery-platform/login";
    };

    return (
        <div className="account-page">
            <div className="dropdown">
                <button className="dropbtn">{user.fullName} ▼</button>
                <div className="dropdown-content">
                    <p><strong>Profile:</strong> {user.fullName}</p>
                    <p><strong>Email:</strong> {user.email}</p>
                    <h4>Accounts:</h4>
                    <ul>
                        {user.accounts.map((acc, index) => (
                            <li key={index}>{acc}</li>
                        ))}
                    </ul>
                    <button className="logout-btn" onClick={handleLogout}>🚪 Logout</button>
                </div>
            </div>
        </div>
    );
};

export default AccountPage;
