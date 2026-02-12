import React, { useState } from "react";
import { createAccount } from "../../api/Account.jsx";
import { useNavigate } from "react-router-dom";
import { useUser } from "../../context/UserContext";
import usePhotoUpload from "../../hooks/usePhotoUpload";
import "./styles/AccountForm.css";

export default function AccountFormBase({
    initialState,
    buildAccountPayload,
    endpoint,
    submitText,
    children
    }) {
    const navigate = useNavigate();
    const { reloadUser } = useUser();

    const [formData, setFormData] = useState(initialState);

    const changeField = (e) => {
        const { name, value } = e.target;
        setFormData(prev => ({ ...prev, [name]: value }));
    };

    const {
        dropRef,
        onDrop,
        onDragOver,
        onDragLeave,
        onFileChange,
        removePhoto
    } = usePhotoUpload(setFormData);

    const handleSubmit = async (e) => {
        e.preventDefault();

        const payload = buildAccountPayload(formData);

        try {
            await createAccount(endpoint, payload);
            await reloadUser();
            navigate("/profile");
        } catch (err) {
            console.error(err);
            alert("Failed to create account");
        }
    };

    return (
        <form className="account-form" onSubmit={handleSubmit}>
            {children(formData, changeField)}

            <div
                ref={dropRef}
                className={`file-input-wrapper file-label ${formData.photoPreview ? "has-preview" : ""}`}
                onDrop={onDrop}
                onDragOver={onDragOver}
                onDragLeave={onDragLeave}
                onClick={() => dropRef.current?.querySelector('input[type="file"]')?.click()}
                role="button"
            >
                {formData.photoPreview ? (
                    <>
                        <img src={formData.photoPreview} alt="preview" className="drop-preview" />
                        <div className="preview-actions">
                            <button type="button" className="remove-address-btn" onClick={removePhoto}>
                                Remove
                            </button>
                        </div>
                    </>
                ) : (
                    <div className="upload-placeholder">
                        <div className="upload-title">
                            Drag photo here or click to select
                        </div>
                        <div className="upload-subtitle">
                            PNG / JPG, up to 5MB
                        </div>
                    </div>
                )}

                <input
                    type="file"
                    accept="image/*"
                    className="file-input"
                    onChange={onFileChange}
                />
            </div>

            <button type="submit">{submitText}</button>
        </form>
    );
}
