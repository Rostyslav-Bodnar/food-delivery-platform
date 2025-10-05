import React, { useState } from "react";
import "../styles/FormBase.css";

const CourierForm = () => {
    const [formData, setFormData] = useState({
        firstName: "",
        lastName: "",
        phone: "",
        address: "",
        description: "",
        requisites: "",
        photo: null,
        paymentMethod: "Cash",
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
        alert("Courier account created successfully!");
        console.log(formData);
    };

    return (
        <form className="account-form" onSubmit={handleSubmit}>
            <input type="text" name="firstName" placeholder="First Name" value={formData.firstName} onChange={handleChange} required />
            <input type="text" name="lastName" placeholder="Last Name" value={formData.lastName} onChange={handleChange} required />
            <input type="tel" name="phone" placeholder="Phone Number" value={formData.phone} onChange={handleChange} required />
            <input type="text" name="address" placeholder="Work Area / City" value={formData.address} onChange={handleChange} required />
            <textarea name="description" placeholder="Short Description (experience, delivery type...)" value={formData.description} onChange={handleChange} className="textarea-input" />
            <input type="text" name="requisites" placeholder="Payment Requisites" value={formData.requisites} onChange={handleChange} />
            <div className="file-input-wrapper">
                <label className="file-label">Upload Profile Photo</label>
                <input type="file" name="photo" accept="image/*" onChange={handleChange} className="file-input" />
            </div>
            <select name="paymentMethod" value={formData.paymentMethod} onChange={handleChange}>
                <option value="Cash">💵 Cash</option>
                <option value="Card">💳 Card</option>
            </select>
            <button type="submit">Create Courier Account</button>
        </form>
    );
};

export default CourierForm;