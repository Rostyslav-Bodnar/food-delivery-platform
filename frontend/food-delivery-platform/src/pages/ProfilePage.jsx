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
    const [currentAccountId, setCurrentAccountId] = useState(1); // Track active account
    const inputRef = useRef(null);
    const fetchedRef = useRef(false);

    // Mock data for active accounts
    const mockAccounts = [
        {
            id: 1,
            name: "John Doe",
            email: "john.doe@example.com",
            type: "Admin",
            phone: "+1234567890",
            address: "123 Main St",
            avatar: null,
        },
        {
            id: 2,
            name: "Jane Smith",
            email: "jane.smith@example.com",
            type: "User",
            phone: "—",
            address: "456 Elm St",
            avatar: null,
        },
        {
            id: 3,
            name: "Alex Johnson",
            email: "alex.johnson@example.com",
            type: "Guest",
            phone: "—",
            address: "—",
            avatar: null,
        },
    ];

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

        // For demo, use mock data as initial user
        const initialUser = mockAccounts.find((account) => account.id === currentAccountId);
        setUser(initialUser);
        setFormData({
            name: initialUser.name,
            phone: initialUser.phone === "—" ? "" : initialUser.phone,
            address: initialUser.address === "—" ? "" : initialUser.address,
            avatar: initialUser.avatar,
        });
        setLoading(false);

        // Uncomment to fetch real user data
        // fetchUser();
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
            // Update mockAccounts to reflect changes (for demo purposes)
            const updatedAccounts = mockAccounts.map((account) =>
                account.id === user.id ? { ...account, [field]: formData[field] || "—" } : account
            );
            // Normally, you'd update via API:
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

    const handleAccountSwitch = (account) => {
        setCurrentAccountId(account.id);
        setUser(account);
        setFormData({
            name: account.name,
            phone: account.phone === "—" ? "" : account.phone,
            address: account.address === "—" ? "" : account.address,
            avatar: account.avatar,
        });
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
                    <div className="active-accounts">
                        <h3>Active Accounts</h3>
                        <ul>
                            {mockAccounts.map((account) => (
                                <li
                                    key={account.id}
                                    className={account.id === currentAccountId ? "active-account" : ""}
                                    onClick={() => handleAccountSwitch(account)}
                                >
                                    <div className="account-avatar">
                                        {account.avatar ? (
                                            <img src={account.avatar} alt="Account Avatar" className="account-avatar-image" />
                                        ) : (
                                            account.name[0]
                                        )}
                                    </div>
                                    <span className="account-name">{account.name}</span>
                                    <span className="account-type">({account.type})</span>
                                </li>
                            ))}
                        </ul>
                    </div>
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