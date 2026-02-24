// src/pages/ProfilePage.js (or wherever the original is)
import React from "react";
import { useUser } from "../../context/UserContext";
import useProfileForm from "./hooks/useProfileForm";
import useAccountSwitch from "./hooks/useAccountSwitch";
import usePaymentCards from "./hooks/usePaymentCards";
import useBusinessAddresses from "./hooks/useBusinessAddresses";
import UserCard from "./components/UserCard";
import PaymentCards from "./components/PaymentCards";
import BusinessAddresses from "./components/BusinessAddresses";

import "./styles/ProfilePage.css";

const ProfilePage = () => {
    const { accounts, currentAccountId, loading, user } = useUser();

    const {
        error,
        editingField,
        formData,
        isAvatarHovered,
        setIsAvatarHovered,
        inputRef,
        handleEditToggle,
        handleInputChange,
        handleAvatarChange,
        handleSave,
    } = useProfileForm();

    const { handleAccountSwitch } = useAccountSwitch();

    const {
        paymentCards,
        activeCardId,
        editingCardId,
        cardForm,
        cardSuccess,
        selectCard,
        startEditingCard,
        deleteCard,
        handleCardInputChange,
        handleCardSubmit,
        cancelCardEdit,
        startAddingCard,
    } = usePaymentCards();

    const {
        businessAddresses,
        editingAddressId,
        addressForm,
        startAddingAddress,
        startEditingAddress,
        deleteAddress,
        handleAddressSubmit,
        cancelAddressEdit,
        setAddressForm,
    } = useBusinessAddresses();

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