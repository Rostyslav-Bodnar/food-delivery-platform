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
        clearError,
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
        loadingLocations,
        loadError,
        addressForm,
        searchQuery,
        searchResults,
        searching,
        searchError,
        submitting,
        submitError,
        submitSuccess,
        isComposerOpen,
        mapCenter,
        isResolvingPoint,
        openComposer,
        closeComposer,
        handleAddressFieldChange,
        selectSuggestion,
        selectPointOnMap,
        handleSubmit,
    } = useBusinessAddresses();
    
    if (loading) return <>Loading profile...</>;

    const currentAccount = accounts.find(a => a.id === currentAccountId);
    const isCustomer = currentAccount?.accountType === "Customer";
    const isBusiness = currentAccount?.accountType === "Business";

    return (
        <div className="page-wrapper">
            
            <div className="user-container">
                <h2>Profile</h2>

                <div className="user-info">
                    <div className="user-top">

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

                        <div className="active-accounts">
                            <h3>Accounts</h3>
                            <ul>
                                {accounts.map((account) => (
                                    <li
                                        key={account.id}
                                        className={
                                            account.id === currentAccountId
                                                ? "active-account"
                                                : ""
                                        }
                                        onClick={() => handleAccountSwitch(account)}
                                    >
                                        <div className="account-avatar">
                                            {account.imageUrl ? (
                                                <img
                                                    src={account.imageUrl}
                                                    alt={account.name}
                                                    className="account-avatar-image"
                                                />
                                            ) : (
                                                <div className="avatar-initial">
                                                    {account.name?.[0] ?? "U"}
                                                </div>
                                            )}
                                        </div>

                                        <div>
                                            {account.name} ({account.accountType})
                                        </div>
                                    </li>
                                ))}
                            </ul>
                        </div>
                    </div>

                    {/* ✅ BUSINESS */}
                    {isBusiness && (
                        <BusinessAddresses
                            businessAddresses={businessAddresses}
                            loadingLocations={loadingLocations}
                            loadError={loadError}
                            addressForm={addressForm}
                            searchQuery={searchQuery}
                            searchResults={searchResults}
                            searching={searching}
                            searchError={searchError}
                            submitting={submitting}
                            submitError={submitError}
                            submitSuccess={submitSuccess}
                            isComposerOpen={isComposerOpen}
                            mapCenter={mapCenter}
                            isResolvingPoint={isResolvingPoint}
                            openComposer={openComposer}
                            closeComposer={closeComposer}
                            handleAddressFieldChange={handleAddressFieldChange}
                            selectSuggestion={selectSuggestion}
                            selectPointOnMap={selectPointOnMap}
                            handleSubmit={handleSubmit}
                        />
                    )}

                    {/* ✅ CUSTOMER */}
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
                </div>

                <div className="user-data" />
            </div>
        </div>
    );
};

export default ProfilePage;