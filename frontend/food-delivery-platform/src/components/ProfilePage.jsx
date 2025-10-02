import React, { useEffect, useState, useRef } from "react";
import { getProfile } from "../api/User.jsx";
import { refresh } from '../api/auth';
import "./styles/ProfilePage.css";

const Profile = () => {
    const [user, setUser] = useState(null);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState(null);
    const fetchedRef = useRef(false); // ✅ флаг, щоб викликати лише один раз

    useEffect(() => {
        if (fetchedRef.current) return; // якщо вже викликали
        fetchedRef.current = true;

        const fetchUser = async () => {
            try {
                let token = localStorage.getItem("accessToken");

                if (!token) {
                    const tokens = await refresh();
                    token = tokens.accessToken;
                }

                if (!token) {
                    throw new Error("No auth token found. Please login.");
                }

                const data = await getProfile(token);
                setUser({
                    id: data.id,
                    name: data.fullName,
                    email: data.email,
                    phone: data.phoneNumber ?? "—",
                    address: data.address ?? "—",
                });
            } catch (err) {
                setError(err.message || "Failed to fetch profile");
            } finally {
                setLoading(false);
            }
        };

        fetchUser();
    }, []);

    if (loading) return <div className="page-wrapper"><div className="user-container">⏳ Loading profile...</div></div>;
    if (error) return <div className="page-wrapper"><div className="user-container">❌ {error}</div></div>;

    return (
        <div className="page-wrapper">
            <div className="user-container">
                <h2>🍔 FoodExpress — Profile</h2>

                <div className="user-card">
                    <div className="user-avatar">{user?.name ? user.name[0] : "U"}</div>
                    <div className="user-info">
                        <p className="user-name">{user?.name}</p>
                        <p><span>📧</span> {user?.email}</p>
                        <p><span>📱</span> {user?.phone}</p>
                        <p><span>📍</span> {user?.address}</p>
                    </div>
                </div>

                <div className="user-actions">
                    <button className="btn primary">✏️ Edit profile</button>
                    <button
                        className="btn secondary"
                        onClick={() => {
                            localStorage.removeItem("accessToken");
                            window.location.href = "/login";
                        }}
                    >
                        🚪 Logout
                    </button>
                </div>
            </div>
        </div>
    );
};

export default Profile;
