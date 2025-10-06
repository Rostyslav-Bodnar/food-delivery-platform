import React, { useState } from "react";
import { createAccount } from "../../api/Account.jsx";
import "../styles/FormBase.css";

const CourierForm = () => {
    const [formData, setFormData] = useState({
        firstName: "",
        lastName: "",
        phone: "",
        address: "",
        description: "",
        photo: null
    });

    const handleChange = (e) => {
        const { name, value, files } = e.target;
        setFormData({
            ...formData,
            [name]: files ? files[0] : value,
        });
    };

    const handleSubmit = async (e) => {
        e.preventDefault();

        const account = {
            accountType: "Courier",
            name: formData.firstName,
            surname: formData.lastName,
            phoneNumber: formData.phone,
            address: formData.address,
            description: formData.description,
            imageUrl: formData.photo ? formData.photo.name : null
        };

        try {
            await createAccount(account);
            alert("Courier account created successfully!");
        } catch (err) {
            console.error(err);
            alert("Failed to create courier account");
        }
    };

    return (
        <form className="account-form" onSubmit={handleSubmit}>
            <input type="text" name="firstName" placeholder="First Name" value={formData.firstName} onChange={handleChange} required />
            <input type="text" name="lastName" placeholder="Last Name" value={formData.lastName} onChange={handleChange} required />
            <input type="tel" name="phone" placeholder="Phone Number" value={formData.phone} onChange={handleChange} required />
            <input type="text" name="address" placeholder="Address" value={formData.address} onChange={handleChange} required />
            <textarea name="description" placeholder="Description" value={formData.description} onChange={handleChange} />
            <div className="file-input-wrapper">
                <label className="file-label">Upload Photo</label>
                <input type="file" name="photo" accept="image/*" onChange={handleChange} className="file-input" />
            </div>
            <button type="submit">Create Courier Account</button>
        </form>
    );
};

export default CourierForm;
