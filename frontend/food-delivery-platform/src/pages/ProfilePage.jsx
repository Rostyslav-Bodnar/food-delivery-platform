import React, { useEffect, useState, useRef } from "react";
import { useUser } from "../context/UserContext";
import { updateProfile } from "../api/Profile.jsx";
import { refresh } from "../api/Auth.jsx";
import UserCard from "../components/profile/UserCard";
import PaymentCards from "../components/profile/PaymentCards";
import BusinessAddresses from "../components/profile/BusinessAddresses";

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
        setCardSuccess("Card was deleted");
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
            setCardSuccess("Card was added!");
        } else {
            setPaymentCards(prev => prev.map(c => c.id === editingCardId ? { ...c, ...cardData } : c));
            setCardSuccess("Card was updated!");
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

    if (loading) return <>Loading profile...</>;
    if (error) return <>Error: {error}</>;

    const currentAccount = accounts.find(a => a.id === currentAccountId);
    const isCustomer = currentAccount?.accountType === "Customer";
    const isBusiness = currentAccount?.accountType === "Business";

    return (
        <div className="page-wrapper">
            <div className="user-container">
                <h2>Profile</h2>

                <UserCard
                    formData={formData}
                    currentAccount={currentAccount}
                    user={user}
                    editingField={editingField}
                    isAvatarHovered={isAvatarHovered}
                    setIsAvatarHovered={setIsAvatarHovered}
                    inputRef={inputRef}
                    handleAvatarChange={handleAvatarChange}
                    handleInputChange={handleInputChange}
                    handleEditToggle={handleEditToggle}
                    handleSave={handleSave}
                />

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

                    {isCustomer && (
                        <PaymentCards
                            paymentCards={paymentCards}
                            activeCardId={activeCardId}
                            editingCardId={editingCardId}
                            cardForm={cardForm}
                            cardSuccess={cardSuccess}
                            selectCard={selectCard}
                            startEditingCard={startEditingCard}
                            deleteCard={deleteCard}
                            handleCardInputChange={handleCardInputChange}
                            handleCardSubmit={handleCardSubmit}
                            cancelCardEdit={cancelCardEdit}
                            startAddingCard={startAddingCard}
                        />
                    )}
                    
                    {isBusiness && (
                        <BusinessAddresses
                            businessAddresses={businessAddresses}
                            editingAddressId={editingAddressId}
                            addressForm={addressForm}
                            startAddingAddress={startAddingAddress}
                            startEditingAddress={startEditingAddress}
                            deleteAddress={deleteAddress}
                            handleAddressSubmit={handleAddressSubmit}
                            cancelAddressEdit={cancelAddressEdit}
                            setAddressForm={setAddressForm}
                        />
                    )}

                </div>
            </div>
        </div>
    );
};

export default ProfilePage;