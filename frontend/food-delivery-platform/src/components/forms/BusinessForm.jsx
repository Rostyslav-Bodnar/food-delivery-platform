import React, { useState } from "react";
import { createAccount } from "../../api/Account.jsx";
import "../styles/FormBase.css";
import { useNavigate } from "react-router-dom";
import { useUser } from "../../context/UserContext";

const BusinessForm = () => {
    const navigate = useNavigate();
    const { reloadUser } = useUser();

    const [formData, setFormData] = useState({
        companyName: "",
        description: "",
        phone: "",
        photo: null
    });

    const handleChange = (e) => {
        const { name, value, files } = e.target;
        setFormData({ ...formData, [name]: files ? files[0] : value });
    };

    const handleSubmit = async (e) => {
        e.preventDefault();

        const account = {
            name: formData.companyName,
            description: formData.description,
            phoneNumber: formData.phone,
            imageFile: formData.photo,
            accountType: 1
        };

        try {
            await createAccount("business", account);
            await reloadUser();
            alert("Business account created successfully!");
            navigate("/profile");
        } catch (err) {
            console.error(err);
            alert("Failed to create business account");
        }
    };

    return (
        <form className="account-form" onSubmit={handleSubmit}>
            <input type="text" name="companyName" placeholder="Business Name" value={formData.companyName} onChange={handleChange} required />
            <textarea name="description" placeholder="Description" value={formData.description} onChange={handleChange} required />
            <input type="tel" name="phone" placeholder="Phone Number" value={formData.phone} onChange={handleChange} required />
            <div className="file-input-wrapper">
                <label className="file-label">Upload Business Photo</label>
                <input type="file" name="photo" accept="image/*" onChange={handleChange} className="file-input" />
            </div>
            <button type="submit">Create Business Account</button>
        </form>
    );
};

export default BusinessForm;
