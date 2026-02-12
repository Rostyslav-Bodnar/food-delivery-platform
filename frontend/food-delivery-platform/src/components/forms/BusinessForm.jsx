import React, { useEffect, useRef, useState } from "react";
import { createAccount } from "../../api/Account.jsx";
import "../styles/FormBase.css";
import { useNavigate } from "react-router-dom";
import { useUser } from "../../context/UserContext";

export default function BusinessForm() {
    const navigate = useNavigate();
    const { reloadUser } = useUser();

    const [formData, setFormData] = useState({
        companyName: "",
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
        // revoke old preview
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

    const handleSubmit = async (e) => {
        e.preventDefault();

        const account = {
            name: formData.companyName,
            description: formData.description,
            imageFile: formData.photoFile,
            accountType: 1
        };

        try {
            await createAccount("business", account);
            await reloadUser();
            navigate("/profile");
        } catch (err) {
            console.error(err);
            alert("Failed to create business account");
        }
    };

    const removePhoto = (e) => {
        e?.preventDefault();
        if (prevUrlRef.current) {
            URL.revokeObjectURL(prevUrlRef.current);
            prevUrlRef.current = null;
        }
        setFormData(prev => ({ ...prev, photoFile: null, photoPreview: "" }));
    };

    return (
        <form className="account-form" onSubmit={handleSubmit}>
            <input
                type="text"
                name="companyName"
                placeholder="Name of the business"
                value={formData.companyName}
                onChange={changeField}
                required
            />

            <textarea
                name="description"
                placeholder="Short description of the business"
                value={formData.description}
                onChange={changeField}
                required
            />

            <div
                ref={dropRef}
                className={`file-input-wrapper file-label ${formData.photoPreview ? "has-preview" : ""}`}
                onDrop={onDrop}
                onDragOver={onDragOver}
                onDragLeave={onDragLeave}
                onClick={() => dropRef.current?.querySelector('input[type="file"]')?.click()}
                role="button"
                aria-label="Upload business photo"
            >
                {formData.photoPreview ? (
                    <>
                        <img src={formData.photoPreview} alt="preview" className="drop-preview" />
                        <div style={{ position: "absolute", right: 12, top: 12, display: "flex", gap: 8 }}>
                            <button
                                type="button"
                                className="remove-address-btn"
                                onClick={removePhoto}
                                title="Delete image"
                            >
                                Delete
                            </button>
                        </div>
                    </>
                ) : (
                    <div style={{ textAlign: "center" }}>
                        <div style={{ fontWeight: 700, color: "var(--muted-2)" }}>Drag and drop image here or click</div>
                        <div style={{ fontSize: 13, color: "var(--muted-2)" }}>PNG / JPG, up to 5MB</div>
                    </div>
                )}

                <input
                    type="file"
                    name="photo"
                    accept="image/*"
                    className="file-input"
                    onChange={onFileChange}
                />
            </div>

            <button type="submit">Create business account</button>
        </form>
    );
}
