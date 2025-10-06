import React, { useEffect, useState, useRef } from "react";
import { getProfileData, switchAccount, updateProfile } from "../api/Profile.jsx";
import { refresh } from "../api/Auth.jsx";
import "./styles/ProfilePage.css";

const ProfilePage = () => {
    const [user, setUser] = useState(null);
    const [accounts, setAccounts] = useState([]);
    const [currentAccountId, setCurrentAccountId] = useState(null);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState(null);
    const [editingField, setEditingField] = useState(null);
    const [formData, setFormData] = useState({ name: "", phone: "", address: "", avatar: null });
    const [isAvatarHovered, setIsAvatarHovered] = useState(false);
    const inputRef = useRef(null);

    // Fetch full profile
    useEffect(() => {
        const fetchProfile = async () => {
            try {
                let token = localStorage.getItem("accessToken");

                if (!token) {
                    const tokens = await refresh();
                    token = tokens.accessToken;
                }

                if (!token) throw new Error("No auth token found. Please login.");

                const data = await getProfileData(token);

                const userData = {
                    id: data.user.id,
                    name: data.user.fullName,
                    email: data.user.email,
                    phone: data.currentAccount?.phoneNumber ?? "—",
                    address: data.currentAccount?.address ?? "—",
                    avatar: data.currentAccount?.imageUrl ?? null,
                };

                setUser(userData);
                setAccounts(data.accounts);
                setCurrentAccountId(data.currentAccount.id);

                setFormData({
                    name: userData.name,
                    phone: userData.phone === "—" ? "" : userData.phone,
                    address: userData.address === "—" ? "" : userData.address,
                    avatar: userData.avatar,
                });
            } catch (err) {
                setError(err.message || "Failed to load profile");
            } finally {
                setLoading(false);
            }
        };

        fetchProfile();
    }, []);

    // Auto-save when clicking outside
    useEffect(() => {
        const handleClickOutside = (event) => {
            if (inputRef.current && !inputRef.current.contains(event.target)) {
                if (editingField) handleSave(editingField);
            }
        };
        document.addEventListener("mousedown", handleClickOutside);
        return () => document.removeEventListener("mousedown", handleClickOutside);
    }, [editingField, formData]);

    const handleEditToggle = (field) => setEditingField(field);

    const handleInputChange = (e) => {
        const { name, value } = e.target;
        setFormData((prev) => ({ ...prev, [name]: value }));
    };

    const handleAvatarChange = async (e) => {
        const file = e.target.files[0];
        if (!file) return;

        const avatarUrl = URL.createObjectURL(file);
        setFormData((prev) => ({ ...prev, avatar: avatarUrl }));
        setUser((prev) => ({ ...prev, avatar: avatarUrl }));
        setIsAvatarHovered(false);

        // Optional: upload avatar via API
        // const formData = new FormData();
        // formData.append("avatar", file);
        // await updateProfile(user.id, formData, token);
    };

    const handleSave = async (field) => {
        try {
            let token = localStorage.getItem("accessToken");
            if (!token) {
                const tokens = await refresh();
                token = tokens.accessToken;
            }

            const updatedUser = {
                ...user,
                [field]: formData[field] || "—",
            };

            // знайди поточний акаунт
            const currentAccount = accounts.find(a => a.id === currentAccountId);

            await updateProfile(user.id, {
                id: currentAccount.id,
                userId: currentAccount.userId,
                accountType: currentAccount.accountType,
                name: field === "name" ? formData.name : currentAccount.name,
                surname: field === "surname" ? formData.surname : currentAccount.surname,
                phoneNumber: field === "phone" ? formData.phone || null : currentAccount.phone,
                address: field === "address" ? formData.address || null : currentAccount.address,
                imageUrl: formData.avatar || user.avatar
            }, token);

            setUser(updatedUser);
            setEditingField(null);
        } catch (err) {
            setError(err.message || "Failed to save profile");
        }
    };


    const handleAccountSwitch = async (account) => {
        try {
            let token = localStorage.getItem("accessToken");
            if (!token) {
                const tokens = await refresh();
                token = tokens.accessToken;
            }

            await switchAccount(account.id, token);
            setCurrentAccountId(account.id);
            setUser({
                id: account.id,
                name: account.name,
                email: user.email,
                phone: account.phoneNumber ?? "—",
                address: account.address ?? "—",
                avatar: account.imageUrl ?? null,
            });

            setFormData({
                name: account.name,
                phone: account.phoneNumber ?? "",
                address: account.address ?? "",
                avatar: account.imageUrl,
            });
        } catch (err) {
            setError(err.message || "Failed to switch account");
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
                            <>
                                <div className="avatar-tooltip">Edit</div>
                                <input
                                    type="file"
                                    accept="image/*"
                                    onChange={handleAvatarChange}
                                    className="avatar-input"
                                />
                            </>
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
                                    ref={inputRef}
                                    autoFocus
                                />
                            </div>
                        ) : (
                            <p className="user-name">
                                {user?.name}
                                <button className="field-edit-btn" onClick={() => handleEditToggle("name")}>✏️</button>
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
                                    ref={inputRef}
                                    autoFocus
                                />
                            </div>
                        ) : (
                            <p><span>📱</span> {user?.phone}
                                <button className="field-edit-btn" onClick={() => handleEditToggle("phone")}>✏️</button>
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
                                    ref={inputRef}
                                    autoFocus
                                />
                            </div>
                        ) : (
                            <p><span>📍</span> {user?.address}
                                <button className="field-edit-btn" onClick={() => handleEditToggle("address")}>✏️</button>
                            </p>
                        )}
                    </div>
                </div>

                <div className="user-actions">
                    <div className="active-accounts">
                        <h3>Active Accounts</h3>
                        <ul>
                            {accounts.map((account) => (
                                <li
                                    key={account.id}
                                    className={account.id === currentAccountId ? "active-account" : ""}
                                    onClick={() => handleAccountSwitch(account)}
                                >
                                    <div className="account-avatar">
                                        {account.imageUrl ? (
                                            <img src={account.imageUrl} alt="Account Avatar" className="account-avatar-image" />
                                        ) : (
                                            (account?.name?.[0] ?? "U")
                                        )}
                                    </div>
                                    <span className="account-name">{account.name}</span>
                                    <span className="account-type">({account.accountType})</span>
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

export default ProfilePage;
