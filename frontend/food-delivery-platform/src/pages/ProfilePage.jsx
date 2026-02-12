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

    // Стан для карток (Customer)
    const [paymentCards, setPaymentCards] = useState([]);
    const [activeCardId, setActiveCardId] = useState(null);
    const [editingCardId, setEditingCardId] = useState(null);
    const [cardForm, setCardForm] = useState({
        cardNumber: "",
        expiryDate: "",
        cvv: "",
        cardHolder: ""
    });
    const [cardSuccess, setCardSuccess] = useState(null);

    // Стан для адрес закладів (Business)
    const [businessAddresses, setBusinessAddresses] = useState([]);
    const [editingAddressId, setEditingAddressId] = useState(null);
    const [addressForm, setAddressForm] = useState({ address: "" });

    const accountTypeMap = { Customer: 0, Business: 1, Courier: 2 };

    useEffect(() => {
        if (user && accounts && currentAccountId) {
            const currentAccount = accounts.find(a => a.id === currentAccountId);
            if (currentAccount) {
                setFormData({
                    name: currentAccount?.name || user.name || "",
                    phone: currentAccount?.phoneNumber || "",
                    address: currentAccount?.address || "",
                    avatar: currentAccount?.imageUrl || null,
                });

                // Ініціалізація платіжних карток
                if (paymentCards.length === 0 && Array.isArray(currentAccount.paymentCards)) {
                    const cards = currentAccount.paymentCards.map((card, index) => ({
                        id: card.id || `card-${index}`,
                        last4: card.last4 || "0000",
                        expiryDate: card.expiryDate || "MM/YY",
                        cardHolder: card.cardHolder || "CARD HOLDER",
                    }));
                    setPaymentCards(cards);
                    if (cards.length > 0) {
                        setActiveCardId(cards[0].id);
                    }
                }

                // Ініціалізація адрес для бізнес-акаунту
                if (currentAccount.accountType === "Business" && currentAccount.businessAddresses) {
                    const addresses = currentAccount.businessAddresses.map((addr, index) => ({
                        id: addr.id || `addr-${index}`,
                        address: addr.address || addr
                    }));
                    setBusinessAddresses(addresses);
                } else if (currentAccount.accountType !== "Business") {
                    setBusinessAddresses([]);
                }
            }
        }
    }, [user, accounts, currentAccountId, paymentCards.length]);

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

    // === Функції для карток ===
    const formatCardNumber = (value) =>
        value.replace(/\D/g, "").match(/.{1,4}/g)?.join(" ") || "";

    const handleCardInputChange = (e) => {
        const { name, value } = e.target;
        let formatted = value;
        if (name === "cardNumber") formatted = formatCardNumber(value);
        if (name === "expiryDate") formatted = value.replace(/\D/g, "").slice(0, 4);
        if (name === "cvv") formatted = value.replace(/\D/g, "").slice(0, 4);
        if (name === "cardHolder") formatted = value.toUpperCase();
        setCardForm(prev => ({ ...prev, [name]: formatted }));
    };

    const startAddingCard = () => {
        setEditingCardId("new");
        setCardForm({ cardNumber: "", expiryDate: "", cvv: "", cardHolder: "" });
    };

    const startEditingCard = (card) => {
        setEditingCardId(card.id);
        setCardForm({
            cardNumber: "",
            expiryDate: card.expiryDate.replace("/", ""),
            cvv: "",
            cardHolder: card.cardHolder
        });
    };

    const deleteCard = (id) => {
        setPaymentCards(prev => prev.filter(c => c.id !== id));
        if (activeCardId === id) {
            const remaining = paymentCards.filter(c => c.id !== id);
            setActiveCardId(remaining[0]?.id || null);
        }
        setCardSuccess("Картку видалено");
        setTimeout(() => setCardSuccess(null), 3000);
    };

    const handleCardSubmit = (e) => {
        e.preventDefault();
        const cleanNumber = cardForm.cardNumber.replace(/\s/g, "");
        if (editingCardId === "new" && cleanNumber.length !== 16) return;

        const formattedExpiry = cardForm.expiryDate.length === 4
            ? `${cardForm.expiryDate.slice(0, 2)}/${cardForm.expiryDate.slice(2)}`
            : "MM/YY";

        const cardData = {
            last4: cleanNumber.slice(-4) || paymentCards.find(c => c.id === editingCardId)?.last4 || "0000",
            expiryDate: formattedExpiry,
            cardHolder: cardForm.cardHolder.trim() || "CARD HOLDER",
        };

        if (editingCardId === "new") {
            const newCard = { id: Date.now().toString(), ...cardData };
            setPaymentCards(prev => [...prev, newCard]);
            setActiveCardId(newCard.id);
            setCardSuccess("Картку додано!");
        } else {
            setPaymentCards(prev => prev.map(c => c.id === editingCardId ? { ...c, ...cardData } : c));
            setCardSuccess("Картку оновлено!");
        }

        setEditingCardId(null);
        setCardForm({ cardNumber: "", expiryDate: "", cvv: "", cardHolder: "" });
        setTimeout(() => setCardSuccess(null), 3000);
    };

    const cancelCardEdit = () => {
        setEditingCardId(null);
        setCardForm({ cardNumber: "", expiryDate: "", cvv: "", cardHolder: "" });
    };

    const selectCard = (id) => setActiveCardId(id);

    // === Функції для адрес бізнесу ===
    const startAddingAddress = () => {
        setEditingAddressId("new");
        setAddressForm({ address: "" });
    };

    const startEditingAddress = (addr) => {
        setEditingAddressId(addr.id);
        setAddressForm({ address: addr.address });
    };

    const deleteAddress = (id) => {
        setBusinessAddresses(prev => prev.filter(a => a.id !== id));
    };

    const handleAddressSubmit = (e) => {
        e.preventDefault();
        if (!addressForm.address.trim()) return;

        if (editingAddressId === "new") {
            const newAddr = {
                id: Date.now().toString(),
                address: addressForm.address.trim()
            };
            setBusinessAddresses(prev => [...prev, newAddr]);
        } else {
            setBusinessAddresses(prev => prev.map(a =>
                a.id === editingAddressId ? { ...a, address: addressForm.address.trim() } : a
            ));
        }

        setEditingAddressId(null);
        setAddressForm({ address: "" });
    };

    const cancelAddressEdit = () => {
        setEditingAddressId(null);
        setAddressForm({ address: "" });
    };

    if (loading) return <>⏳ Loading profile...</>;
    if (error) return <>❌ {error}</>;

    const currentAccount = accounts.find(a => a.id === currentAccountId);
    const isCustomer = currentAccount?.accountType === "Customer";
    const isBusiness = currentAccount?.accountType === "Business";

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
                            <img src={formData.avatar} alt="Avatar" className="avatar-image" />
                        ) : (
                            <div className="avatar-initial">{currentAccount?.name?.[0] ?? "U"}</div>
                        )}
                        {isAvatarHovered && (
                            <>
                                <div className="avatar-tooltip">Edit</div>
                                <input
                                    type="file"
                                    accept="image/*"
                                    className="avatar-input"
                                    onChange={handleAvatarChange}
                                />
                            </>
                        )}
                    </div>

                    <div className="meta">
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
                            <div className="user-name">
                                {formData.name}
                                <button className="field-edit-btn" onClick={() => handleEditToggle("name")}>✏️</button>
                            </div>
                        )}
                        <p>📧 {user?.email}</p>

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
                            <p>
                                📱 {formData.phone || "—"}
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
                            <p>
                                📍 {formData.address || "—"}
                                <button className="field-edit-btn" onClick={() => handleEditToggle("address")}>✏️</button>
                            </p>
                        )}
                    </div>
                </div>

                <div className="user-info">
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
                                            <img src={account.imageUrl} alt={account.name} className="account-avatar-image" />
                                        ) : (
                                            <div className="avatar-initial">{account.name?.[0] ?? "U"}</div>
                                        )}
                                    </div>
                                    <div>
                                        {account.name} ({account.accountType})
                                    </div>
                                </li>
                            ))}
                        </ul>
                    </div>

                    {/* Блок платіжних карток — тільки для Customer */}
                    {isCustomer && (
                        <div style={{ marginTop: "24px" }}>
                            <h3 style={{ color: "#fff" }}>💳 Банківські картки</h3>
                            {cardSuccess && (
                                <div style={{ color: "#52c41a", marginBottom: "12px", fontWeight: "500" }}>
                                    {cardSuccess}
                                </div>
                            )}

                            {paymentCards.map((card) => (
                                <div
                                    key={card.id}
                                    style={{
                                        marginBottom: "12px",
                                        padding: "16px",
                                        background: activeCardId === card.id ? "#1f3a5f" : "#2d2d2d",
                                        borderRadius: "12px",
                                        border: activeCardId === card.id ? "2px solid #1890ff" : "1px solid #434343",
                                        cursor: "pointer",
                                        boxShadow: "0 4px 12px rgba(0,0,0,0.3)"
                                    }}
                                    onClick={() => selectCard(card.id)}
                                >
                                    <p style={{ margin: "0 0 8px 0", fontSize: "16px", fontWeight: "500", color: "#fff" }}>
                                        •••• •••• •••• {card.last4}
                                        <button
                                            onClick={(e) => {
                                                e.stopPropagation();
                                                startEditingCard(card);
                                            }}
                                            style={{ marginLeft: "12px", background: "none", border: "none", color: "#69b1ff", fontSize: "14px", cursor: "pointer" }}
                                        >
                                            ✏️ Змінити
                                        </button>
                                        <button
                                            onClick={(e) => {
                                                e.stopPropagation();
                                                deleteCard(card.id);
                                            }}
                                            style={{ marginLeft: "8px", background: "none", border: "none", color: "#ff4d4f", fontSize: "14px", cursor: "pointer" }}
                                        >
                                            🗑️ Видалити
                                        </button>
                                    </p>
                                    <p style={{ margin: "4px 0", color: "#d0d0d0" }}>Термін дії: {card.expiryDate}</p>
                                    <p style={{ margin: "4px 0", color: "#d0d0d0" }}>Власник: {card.cardHolder}</p>
                                </div>
                            ))}

                            {editingCardId && (
                                <div style={{
                                    padding: "16px",
                                    background: "#2d2d2d",
                                    borderRadius: "12px",
                                    border: "1px solid #434343",
                                    marginBottom: "16px",
                                    boxShadow: "0 4px 12px rgba(0,0,0,0.4)"
                                }}>
                                    <form onSubmit={handleCardSubmit}>
                                        <div style={{ marginBottom: "16px" }}>
                                            <input
                                                type="text"
                                                name="cardHolder"
                                                placeholder="Ім'я власника"
                                                value={cardForm.cardHolder}
                                                onChange={handleCardInputChange}
                                                required
                                                style={{
                                                    width: "100%",
                                                    padding: "12px",
                                                    fontSize: "15px",
                                                    background: "#1e1e1e",
                                                    color: "#fff",
                                                    border: "1px solid #434343",
                                                    borderRadius: "8px"
                                                }}
                                            />
                                        </div>

                                        {editingCardId === "new" && (
                                            <div style={{ marginBottom: "16px" }}>
                                                <input
                                                    type="text"
                                                    name="cardNumber"
                                                    placeholder="Номер карти (наприклад: 4242 4242 4242 4242)"
                                                    value={cardForm.cardNumber}
                                                    onChange={handleCardInputChange}
                                                    required
                                                    maxLength="19"
                                                    style={{
                                                        width: "100%",
                                                        padding: "12px",
                                                        fontSize: "15px",
                                                        background: "#1e1e1e",
                                                        color: "#fff",
                                                        border: "1px solid #434343",
                                                        borderRadius: "8px"
                                                    }}
                                                />
                                            </div>
                                        )}

                                        <div style={{ display: "flex", gap: "12px", marginBottom: "16px" }}>
                                            <input
                                                type="text"
                                                name="expiryDate"
                                                placeholder="MMYY"
                                                value={cardForm.expiryDate}
                                                onChange={handleCardInputChange}
                                                required
                                                maxLength="4"
                                                style={{
                                                    flex: 1,
                                                    padding: "12px",
                                                    fontSize: "15px",
                                                    background: "#1e1e1e",
                                                    color: "#fff",
                                                    border: "1px solid #434343",
                                                    borderRadius: "8px"
                                                }}
                                            />
                                            <input
                                                type="password"
                                                name="cvv"
                                                placeholder="CVV"
                                                value={cardForm.cvv}
                                                onChange={handleCardInputChange}
                                                required={editingCardId === "new"}
                                                maxLength="4"
                                                style={{
                                                    width: "100px",
                                                    padding: "12px",
                                                    fontSize: "15px",
                                                    background: "#1e1e1e",
                                                    color: "#fff",
                                                    border: "1px solid #434343",
                                                    borderRadius: "8px"
                                                }}
                                                autoComplete="off"
                                            />
                                        </div>

                                        <button
                                            type="submit"
                                            style={{
                                                width: "100%",
                                                padding: "14px",
                                                background: "#1890ff",
                                                color: "white",
                                                border: "none",
                                                borderRadius: "8px",
                                                fontSize: "16px",
                                                fontWeight: "500",
                                                cursor: "pointer"
                                            }}
                                        >
                                            {editingCardId === "new" ? "Додати картку" : "Зберегти зміни"}
                                        </button>

                                        <button
                                            type="button"
                                            onClick={cancelCardEdit}
                                            style={{
                                                width: "100%",
                                                padding: "12px",
                                                marginTop: "10px",
                                                background: "#434343",
                                                color: "#fff",
                                                border: "none",
                                                borderRadius: "8px",
                                                fontSize: "15px",
                                                cursor: "pointer"
                                            }}
                                        >
                                            Скасувати
                                        </button>
                                    </form>
                                </div>
                            )}

                            {!editingCardId && (
                                <button
                                    onClick={startAddingCard}
                                    style={{
                                        width: "100%",
                                        padding: "16px",
                                        background: "#1890ff",
                                        color: "white",
                                        border: "none",
                                        borderRadius: "12px",
                                        fontSize: "16px",
                                        fontWeight: "500",
                                        cursor: "pointer",
                                        marginTop: paymentCards.length > 0 ? "12px" : "0",
                                        boxShadow: "0 4px 12px rgba(24, 144, 255, 0.4)"
                                    }}
                                >
                                    ➕ Додати картку
                                </button>
                            )}
                        </div>
                    )}

                    {/* Блок адрес закладів — тільки для Business */}
                    {isBusiness && (
                        <div style={{ marginTop: "24px" }}>
                            <h3 style={{ color: "#fff" }}>🏪 Адреси закладів</h3>

                            {businessAddresses.map((addr) => (
                                <div
                                    key={addr.id}
                                    style={{
                                        position: "relative",
                                        marginBottom: "12px",
                                        padding: "16px",
                                        background: "#2d2d2d",
                                        borderRadius: "12px",
                                        border: "1px solid #434343",
                                        boxShadow: "0 4px 12px rgba(0,0,0,0.3)"
                                    }}
                                >
                                    <p style={{ margin: "0 0 8px 0", color: "#fff", fontSize: "16px" }}>
                                        📍 {addr.address}
                                    </p>
                                    <button
                                        onClick={() => startEditingAddress(addr)}
                                        style={{
                                            position: "absolute",
                                            top: "12px",
                                            right: "120px",  // ← Змінено з 68px на 100px — тепер не наїжджає
                                            background: "none",
                                            border: "none",
                                            color: "#69b1ff",
                                            fontSize: "14px",
                                            cursor: "pointer"
                                        }}
                                    >
                                        ✏️ Змінити
                                    </button>
                                    <button
                                        onClick={() => deleteAddress(addr.id)}
                                        style={{
                                            position: "absolute",
                                            top: "12px",
                                            right: "12px",
                                            background: "none",
                                            border: "none",
                                            color: "#ff4d4f",
                                            fontSize: "14px",
                                            cursor: "pointer"
                                        }}
                                    >
                                        🗑️ Видалити
                                    </button>
                                </div>
                            ))}

                            {editingAddressId && (
                                <div style={{
                                    padding: "16px",
                                    background: "#2d2d2d",
                                    borderRadius: "12px",
                                    border: "1px solid #434343",
                                    marginBottom: "16px",
                                    boxShadow: "0 4px 12px rgba(0,0,0,0.4)"
                                }}>
                                    <form onSubmit={handleAddressSubmit}>
                                        <input
                                            type="text"
                                            placeholder="Введіть адресу закладу"
                                            value={addressForm.address}
                                            onChange={(e) => setAddressForm({ address: e.target.value })}
                                            required
                                            autoFocus
                                            style={{
                                                width: "100%",
                                                padding: "12px",
                                                fontSize: "15px",
                                                background: "#1e1e1e",
                                                color: "#fff",
                                                border: "1px solid #434343",
                                                borderRadius: "8px",
                                                marginBottom: "12px"
                                            }}
                                        />
                                        <div style={{ display: "flex", gap: "12px" }}>
                                            <button
                                                type="submit"
                                                style={{
                                                    flex: 1,
                                                    padding: "14px",
                                                    background: "#1890ff",
                                                    color: "white",
                                                    border: "none",
                                                    borderRadius: "8px",
                                                    fontSize: "16px",
                                                    fontWeight: "500",
                                                    cursor: "pointer"
                                                }}
                                            >
                                                {editingAddressId === "new" ? "Додати адресу" : "Зберегти"}
                                            </button>
                                            <button
                                                type="button"
                                                onClick={cancelAddressEdit}
                                                style={{
                                                    flex: 1,
                                                    padding: "14px",
                                                    background: "#434343",
                                                    color: "#fff",
                                                    border: "none",
                                                    borderRadius: "8px",
                                                    fontSize: "16px",
                                                    cursor: "pointer"
                                                }}
                                            >
                                                Скасувати
                                            </button>
                                        </div>
                                    </form>
                                </div>
                            )}

                            {!editingAddressId && (
                                <button
                                    onClick={startAddingAddress}
                                    style={{
                                        width: "100%",
                                        padding: "16px",
                                        background: "#1890ff",
                                        color: "white",
                                        border: "none",
                                        borderRadius: "12px",
                                        fontSize: "16px",
                                        fontWeight: "500",
                                        cursor: "pointer",
                                        marginTop: businessAddresses.length > 0 ? "12px" : "0",
                                        boxShadow: "0 4px 12px rgba(24, 144, 255, 0.4)"
                                    }}
                                >
                                    ➕ Додати адресу закладу
                                </button>
                            )}
                        </div>
                    )}
                </div>
            </div>
        </div>
    );
};

export default ProfilePage;