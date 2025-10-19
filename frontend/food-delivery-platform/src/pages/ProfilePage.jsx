import React, { useEffect, useState, useRef } from "react";
import { useUser } from "../context/UserContext";
import { updateProfile } from "../api/Profile.jsx";
import { refresh } from "../api/Auth.jsx";
import "./styles/ProfilePage.css";

const ProfilePage = () => {
    const {
        user,
        accounts,
        currentAccountId,
        reloadUser,
        switchAccount,
        loading,
    } = useUser();

    const [error, setError] = useState(null);
    const [editingField, setEditingField] = useState(null);
    const [formData, setFormData] = useState({ name: "", phone: "", address: "", avatar: null });
    const [isAvatarHovered, setIsAvatarHovered] = useState(false);
    const inputRef = useRef(null);
    const accountTypeMap = { Customer: 0, Business: 1, Courier: 2 };
    
    // Initialize form after loading data
    useEffect(() => {
        if (user) {
            const currentAccount = accounts.find(a => a.id === currentAccountId);
            setFormData({
                name: currentAccount?.name || user.name || "",
                phone: currentAccount?.phoneNumber || "",
                address: currentAccount?.address || "",
                avatar: currentAccount?.imageUrl || null,
            });
        }
    }, [user, accounts, currentAccountId]);

    const handleEditToggle = (field) => setEditingField(field);

    const handleInputChange = (e) => {
        const { name, value } = e.target;
        setFormData(prev => ({ ...prev, [name]: value }));
    };

    const handleAvatarChange = async (e) => {
        const file = e.target.files[0];
        if (!file) return;

        const avatarUrl = URL.createObjectURL(file);
        setFormData(prev => ({ ...prev, avatar: avatarUrl }));
        setIsAvatarHovered(false);

        try {
            let token = localStorage.getItem("accessToken");
            if (!token) {
                const tokens = await refresh();
                token = tokens.accessToken;
            }

            const currentAccount = accounts.find(a => a.id === currentAccountId);
            if (!currentAccount) throw new Error("No active account found");

            const body = {
                Id: currentAccount.id,
                UserId: currentAccount.userId,
                AccountType: accountTypeMap[currentAccount.accountType] ?? 0,
                Name: formData.name || currentAccount.name,
                PhoneNumber: formData.phone || currentAccount.phoneNumber || "",
                Address: formData.address || currentAccount.address || "",
                Surname: currentAccount.surname || "",
                Description: currentAccount.description || "",
                ImageFile: file
            };

            await updateProfile(currentAccount.accountType.toLowerCase(), body, token);
            await reloadUser();
        } catch (err) {
            setError(err.response?.data || err.message || "Failed to update avatar");
        }
    };


    const handleSave = async (field) => {
        try {
            let token = localStorage.getItem("accessToken");
            if (!token) {
                const tokens = await refresh();
                token = tokens.accessToken;
            }

            const currentAccount = accounts.find(a => a.id === currentAccountId);
            if (!currentAccount) throw new Error("No active account found");

            const body = {
                Id: currentAccount.id,
                UserId: currentAccount.userId,
                AccountType: accountTypeMap[currentAccount.accountType] ?? 0,
                Name: field === "name" ? formData.name : currentAccount.name,
                PhoneNumber: field === "phone" ? formData.phone || "" : currentAccount.phoneNumber || "",
                Address: field === "address" ? formData.address || "" : currentAccount.address || "",
                Surname: currentAccount.surname || "",
                Description: currentAccount.description || "",
            };

            if (inputRef.current?.files?.[0]) {
                body.ImageFile = inputRef.current.files[0];
            }

            await updateProfile(currentAccount.accountType.toLowerCase(), body, token);

            setEditingField(null);
            await reloadUser();
        } catch (err) {
            setError(err.response?.data || err.message || "Failed to save profile");
        }
    };

    const handleAccountSwitch = async (account) => {
        await switchAccount(account.id);
        await reloadUser();
    };

    if (loading) return <div className="page-wrapper"><div className="user-container">⏳ Loading profile...</div></div>;
    if (error) return <div className="page-wrapper"><div className="user-container">❌ {error}</div></div>;

    const currentAccount = accounts.find(a => a.id === currentAccountId);

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
                        {formData.avatar ? (
                            <img src={formData.avatar} alt="User Avatar" className="avatar-image" />
                        ) : (
                            (currentAccount?.name?.[0] ?? "U")
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
                                {formData.name}
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
                            <p><span>📱</span> {formData.phone || "—"}
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
                            <p><span>📍</span> {formData.address || "—"}
                                <button className="field-edit-btn" onClick={() => handleEditToggle("address")}>✏️</button>
                            </p>
                        )}
                    </div>
                </div>

                <div className="user-actions">
                    <div className="active-accounts">
                        <h3>Accounts</h3>
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
                </div>
            </div>
        </div>
    );
};

export default ProfilePage;