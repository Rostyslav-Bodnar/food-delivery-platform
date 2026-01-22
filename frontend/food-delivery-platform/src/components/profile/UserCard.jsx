import React from "react";
import "./styles/UserCard.css";

const UserCard = ({
    formData,
    currentAccount,
    user,
    editingField,
    isAvatarHovered,
    setIsAvatarHovered,
    inputRef,
    handleAvatarChange,
    handleInputChange,
    handleEditToggle,
    handleSave,
                  }) => {
    return (
        <div className="user-card">
            <div
                className="user-avatar"
                onMouseEnter={() => setIsAvatarHovered(true)}
                onMouseLeave={() => setIsAvatarHovered(false)}
            >
                {formData.avatar ? (
                    <img src={formData.avatar} alt="Avatar" className="avatar-image" />
                ) : (
                    <div className="avatar-initial">
                        {currentAccount?.name?.[0] ?? "U"}
                    </div>
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
                        <button
                            className="field-edit-btn"
                            onClick={() => handleEditToggle("name")}
                        >
                            ✏️
                        </button>
                    </div>
                )}

                <p>Email: {user?.email}</p>

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
                        Phone Number: {formData.phone || "—"}
                        <button
                            className="field-edit-btn"
                            onClick={() => handleEditToggle("phone")}
                        >
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
                            ref={inputRef}
                            autoFocus
                        />
                    </div>
                ) : (
                    <p>
                        Address: {formData.address || "—"}
                        <button
                            className="field-edit-btn"
                            onClick={() => handleEditToggle("address")}
                        >
                            ✏️
                        </button>
                    </p>
                )}
            </div>
        </div>
    );
};

export default UserCard;
