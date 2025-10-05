import React, { useState } from "react";
import "../styles/FormBase.css";

const CustomerForm = () => {
    const [formData, setFormData] = useState({
        firstName: "",
        lastName: "",
        phone: "",
        address: "",
        photo: null,
        paymentMethod: "Credit Card",
    });

    const handleChange = (e) => {
        const { name, value, files } = e.target;
        setFormData({
            ...formData,
            [name]: files ? files[0] : value,
        });
    };

    const handleSubmit = (e) => {
        e.preventDefault();
        alert("Customer account created successfully!");
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
            <select name="paymentMethod" value={formData.paymentMethod} onChange={handleChange}>
                <option value="Credit Card">💳 Credit Card</option>
                <option value="PayPal">💰 PayPal</option>
                <option value="Cash">💵 Cash</option>
            </select>
            <button type="submit">Create Account</button>
        </form>
    );
};

export default CustomerForm;