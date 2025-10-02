import React, { useEffect, useState } from "react";
import "./styles/UserPage.css";

const UserPage = () => {
    const [user, setUser] = useState(null);
    const [loading, setLoading] = useState(true);

    useEffect(() => {
        fetch("/api/user")
            .then((r) => {
                if (!r.ok) throw new Error("no-api");
                return r.json();
            })
            .then((data) => {
                setUser(data);
            })
            .catch(() => {
                // Мок-дані
                setUser({
                    id: 1,
                    name: "Vladyslav Drohomereckyy",
                    email: "vlad@example.com",
                    phone: "+380630000000",
                    address: "Kyiv, Ukraine",
                });
            })
            .finally(() => setLoading(false));
    }, []);

    if (loading) {
        return (
            <div className="page-wrapper">
                <div className="user-container">⏳ Loading profile...</div>
            </div>
        );
    }

    return (
        <div className="page-wrapper">
            <div className="user-container">
                <h2>🍔 FoodExpress — Profile</h2>

                <div className="user-card">
                    <div className="user-avatar">
                        {user.name ? user.name[0] : "U"}
                    </div>

                    <div className="user-info">
                        <p className="user-name">{user.name}</p>
                        <p><span>📧</span> {user.email}</p>
                        <p><span>📱</span> {user.phone}</p>
                        <p><span>📍</span> {user.address}</p>
                    </div>
                </div>

                <div className="user-actions">
                    <button className="btn primary">✏️ Edit profile</button>
                    <button className="btn secondary">🚪 Logout</button>
                </div>
            </div>
        </div>
    );
};

export default UserPage;
