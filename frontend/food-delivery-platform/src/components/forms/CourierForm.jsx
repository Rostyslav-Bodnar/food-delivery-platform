import React, { useEffect, useRef, useState } from "react";
import { createAccount } from "../../api/Account.jsx";
import "../styles/FormBase.css";
import { useNavigate } from "react-router-dom";
import { useUser } from "../../context/UserContext";

export default function CourierForm() {
    const navigate = useNavigate();
    const { reloadUser } = useUser();

    const [formData, setFormData] = useState({
        firstName: "",
        lastName: "",
        phone: "",
        address: "",
        description: "",
        photoFile: null,
        photoPreview: ""
    });

    const dropRef = useRef(null);
    const prevUrlRef = useRef(null);

    useEffect(() => {
        return () => {
            if (prevUrlRef.current) {
                URL.revokeObjectURL(prevUrlRef.current);
                prevUrlRef.current = null;
            }
        };
    }, []);

    const changeField = (e) => {
        const { name, value } = e.target;
        setFormData(prev => ({ ...prev, [name]: value }));
    };

    const handleFileSelect = (file) => {
        if (!file) return;
        if (prevUrlRef.current) {
            URL.revokeObjectURL(prevUrlRef.current);
            prevUrlRef.current = null;
        }
        const url = URL.createObjectURL(file);
        prevUrlRef.current = url;
        setFormData(prev => ({ ...prev, photoFile: file, photoPreview: url }));
    };

    const onDrop = (e) => {
        e.preventDefault();
        const f = e.dataTransfer?.files?.[0];
        if (f && f.type.startsWith("image/")) handleFileSelect(f);
        dropRef.current?.classList?.remove("dragover");
    };
    const onDragOver = (e) => { e.preventDefault(); dropRef.current?.classList?.add("dragover"); };
    const onDragLeave = () => { dropRef.current?.classList?.remove("dragover"); };

    const onFileChange = (e) => {
        const f = e.target.files?.[0];
        if (f && f.type.startsWith("image/")) handleFileSelect(f);
    };

    const removePhoto = (e) => {
        e?.preventDefault();
        if (prevUrlRef.current) {
            URL.revokeObjectURL(prevUrlRef.current);
            prevUrlRef.current = null;
        }
        setFormData(prev => ({ ...prev, photoFile: null, photoPreview: "" }));
    };

    const handleSubmit = async (e) => {
        e.preventDefault();

        const account = {
            name: formData.firstName,
            surname: formData.lastName,
            phoneNumber: formData.phone,
            address: formData.address,
            description: formData.description,
            imageFile: formData.photoFile,
            accountType: 2
        };

        try {
            await createAccount("courier", account);
            await reloadUser();
            navigate("/profile");
        } catch (err) {
            console.error(err);
            alert("Failed to create courier account");
        }
    };

    return (
        <form className="account-form" onSubmit={handleSubmit}>
            <div className="account-form-header">
                <input type="text" name="firstName" placeholder="First Name" value={formData.firstName} onChange={changeField} required />
                <input type="text" name="lastName" placeholder="Last Name" value={formData.lastName} onChange={changeField} required />
                <input type="tel" name="phone" placeholder="Phone Number" value={formData.phone} onChange={changeField} required />
            </div>
            <input type="text" name="address" placeholder="Address" value={formData.address} onChange={changeField} required />
            <textarea name="description" placeholder="Description" value={formData.description} onChange={changeField} />

            <div
                ref={dropRef}
                className={`file-input-wrapper file-label ${formData.photoPreview ? "has-preview" : ""}`}
                onDrop={onDrop}
                onDragOver={onDragOver}
                onDragLeave={onDragLeave}
                onClick={() => dropRef.current?.querySelector('input[type="file"]')?.click()}
                role="button"
                aria-label="Upload profile photo"
            >
                {formData.photoPreview ? (
                    <>
                        <img src={formData.photoPreview} alt="preview" className="drop-preview" />
                        <div style={{ position: "absolute", right: 12, top: 12 }}>
                            <button type="button" className="remove-address-btn" onClick={removePhoto}>Remove</button>
                        </div>
                    </>
                ) : (
                    <div style={{ textAlign: "center" }}>
                        <div style={{ fontWeight: 700, color: "var(--muted-2)" }}>Drag photo here or click to select</div>
                        <div style={{ fontSize: 13, color: "var(--muted-2)" }}>PNG / JPG, up to 5 MB</div>
                    </div>
                )}

                <input type="file" name="photo" accept="image/*" className="file-input" onChange={onFileChange} />
            </div>

            <button type="submit">Create Courier Account</button>
        </form>
    );
}
