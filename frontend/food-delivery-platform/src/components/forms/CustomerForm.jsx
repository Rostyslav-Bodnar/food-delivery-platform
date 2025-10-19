import React, { useState } from "react";
import { createAccount } from "../../api/Account.jsx";
import "../styles/FormBase.css";
import { useNavigate } from "react-router-dom";
import { useUser } from "../../context/UserContext";

const CustomerForm = () => {
    const navigate = useNavigate();
    const { reloadUser } = useUser();

    const [formData, setFormData] = useState({
        firstName: "",
        lastName: "",
        phone: "",
        address: "",
        photo: null
    });

    const handleChange = (e) => {
        const { name, value, files } = e.target;
        setFormData({ ...formData, [name]: files ? files[0] : value });
    };

    const handleSubmit = async (e) => {
        e.preventDefault();

        const account = {
            name: formData.firstName,
            surname: formData.lastName,
            phoneNumber: formData.phone,
            address: formData.address,
            imageFile: formData.photo,
            accountType: 0
        };

        try {
            await createAccount("customer", account);
            await reloadUser();
            alert("Customer account created successfully!");
            navigate("/profile");
        } catch (err) {
            console.error(err);
            alert("Failed to create customer account");
        }
    };

    return (
        <form className="account-form" onSubmit={handleSubmit}>
            <input type="text" name="firstName" placeholder="First Name" value={formData.firstName} onChange={handleChange} required />
            <input type="text" name="lastName" placeholder="Last Name" value={formData.lastName} onChange={handleChange} required />
            <input type="tel" name="phone" placeholder="Phone Number" value={formData.phone} onChange={handleChange} required />
            <input type="text" name="address" placeholder="Address" value={formData.address} onChange={handleChange} required />
            <div className="file-input-wrapper">
                <label className="file-label">Upload Profile Photo</label>
                <input type="file" name="photo" accept="image/*" onChange={handleChange} className="file-input" />
            </div>
            <button type="submit">Create Customer Account</button>
        </form>
    );
};

export default CustomerForm;
