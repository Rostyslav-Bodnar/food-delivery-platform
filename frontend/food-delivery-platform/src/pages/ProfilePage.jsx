import React, { useEffect, useState, useRef } from "react";
import { getProfile } from "../api/User.jsx";
import { refresh } from "../api/Auth.jsx";
import "./styles/ProfilePage.css";

const Profile = () => {
    const [user, setUser] = useState(null);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState(null);
    const [editingField, setEditingField] = useState(null);
    const [formData, setFormData] = useState({ name: "", phone: "", address: "", avatar: null });
    const [isAvatarHovered, setIsAvatarHovered] = useState(false);
    const inputRef = useRef(null);
    const fetchedRef = useRef(false);

    useEffect(() => {
        if (fetchedRef.current) return;
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
                const userData = {
                    id: data.id,
                    name: data.fullName,
                    email: data.email,
                    phone: data.phoneNumber ?? "—",
                    address: data.address ?? "—",
                    avatar: null,
                };
                setUser(userData);
                setFormData({
                    name: userData.name,
                    phone: userData.phone === "—" ? "" : userData.phone,
                    address: userData.address === "—" ? "" : userData.address,
                    avatar: null,
                });
            } catch (err) {
                setError(err.message || "Failed to fetch profile");
            } finally {
                setLoading(false);
            }
        };

        fetchUser();
    }, []);

    useEffect(() => {
        const handleClickOutside = (event) => {
            if (inputRef.current && !inputRef.current.contains(event.target)) {
                if (editingField) {
                    handleSave(editingField);
                }
            }
        };

        document.addEventListener("mousedown", handleClickOutside);
        return () => document.removeEventListener("mousedown", handleClickOutside);
    }, [editingField, formData]);

    const handleEditToggle = (field) => {
        setEditingField(field);
    };

    const handleInputChange = (e) => {
        const { name, value } = e.target;
        setFormData((prev) => ({ ...prev, [name]: value }));
    };

    const handleAvatarChange = (e) => {
        const file = e.target.files[0];
        if (file) {
            const avatarUrl = URL.createObjectURL(file);
            setFormData((prev) => ({ ...prev, avatar: avatarUrl }));
            setUser((prev) => ({ ...prev, avatar: avatarUrl }));
            setIsAvatarHovered(false);
        }
    };

    const handleSave = async (field) => {
        try {
            const updatedUser = {
                ...user,
                [field]: formData[field] || "—",
            };
            setUser(updatedUser);
            setEditingField(null);
            // Example API call (uncomment and adjust when API is ready):
            // await updateProfile(user.id, {
            //     fullName: field === "name" ? formData.name : user.name,
            //     phoneNumber: field === "phone" ? (formData.phone || null) : user.phone,
            //     address: field === "address" ? (formData.address || null) : user.address,
            //     avatar: formData.avatar || user.avatar,
            // });
        } catch (err) {
            setError(err.message || "Failed to save profile");
        }
    };

    if (loading) return <div className="page-wrapper"><div className="user-container">⏳ Loading profile...</div></div>;
    if (error) return <div className="page-wrapper"><div className="user-container">❌ {error}</div></div>;

    return (
        <div className="page-wrapper">
            <div className="user-container">
                <h2>🍔 FoodExpress — Profile</h2>

                <div className="user-card">
                    <div
                        className="user-avatar"
                        onMouseEnter={() => setIsAvatarHovered(true)}
                        onMouseLeave={() => setIsAvatarHovered(false)}
                    >
                        {user?.avatar ? (
                            <img src={user.avatar} alt="User Avatar" className="avatar-image" />
                        ) : (
                            user?.name ? user.name[0] : "U"
                        )}
                        {isAvatarHovered && (
                            <div className="avatar-tooltip">Edit</div>
                        )}
                        {isAvatarHovered && (
                            <input
                                type="file"
                                accept="image/*"
                                onChange={handleAvatarChange}
                                className="avatar-input"
                            />
                        )}
                    </div>
                    <div className="user-info">
                        {editingField === "name" ? (
                            <div className="edit-field">
                                <input
                                    type="text"
                                    name="name"
                                    value={formData.name}
                                    onChange={handleInputChange}
                                    onBlur={() => handleSave("name")}
                                    className="edit-input"
                                    placeholder="Enter name"
                                    ref={inputRef}
                                    autoFocus
                                />
                            </div>
                        ) : (
                            <p className="user-name">
                                {user?.name}
                                <button className="field-edit-btn" onClick={() => handleEditToggle("name")}>
                                    ✏️
                                </button>
                            </p>
                        )}
                        <p><span>📧</span> {user?.email}</p>
                        {editingField === "phone" ? (
                            <div className="edit-field">
                                <input
                                    type="text"
                                    name="phone"
                                    value={formData.phone}
                                    onChange={handleInputChange}
                                    onBlur={() => handleSave("phone")}
                                    className="edit-input"
                                    placeholder="Enter phone number"
                                    ref={inputRef}
                                    autoFocus
                                />
                            </div>
                        ) : (
                            <p>
                                <span>📱</span> {user?.phone}
                                <button className="field-edit-btn" onClick={() => handleEditToggle("phone")}>
                                    ✏️
                                </button>
                            </p>
                        )}
                        {editingField === "address" ? (
                            <div className="edit-field">
                                <input
                                    type="text"
                                    name="address"
                                    value={formData.address}
                                    onChange={handleInputChange}
                                    onBlur={() => handleSave("address")}
                                    className="edit-input"
                                    placeholder="Enter address"
                                    ref={inputRef}
                                    autoFocus
                                />
                            </div>
                        ) : (
                            <p>
                                <span>📍</span> {user?.address}
                                <button className="field-edit-btn" onClick={() => handleEditToggle("address")}>
                                    ✏️
                                </button>
                            </p>
                        )}
                    </div>
                </div>

                <div className="user-actions">
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