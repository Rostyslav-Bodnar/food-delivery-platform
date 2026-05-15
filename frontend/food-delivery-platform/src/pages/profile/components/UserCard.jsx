import React from "react";
import "../styles/UserCard.css";

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
                <p>Phone Number: {formData.phone || "+111111111111"}</p>
            </div>
        </div>
    );
};

export default UserCard;
