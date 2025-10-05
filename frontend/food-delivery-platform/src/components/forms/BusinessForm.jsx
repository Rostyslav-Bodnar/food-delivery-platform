import React, { useState } from "react";
import "../styles/FormBase.css";

const BusinessForm = () => {
    const [formData, setFormData] = useState({
        companyName: "",
        description: "",
        phone: "",
        addresses: [""],
        requisites: "",
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

    const handleAddressChange = (index, value) => {
        const newAddresses = [...formData.addresses];
        newAddresses[index] = value;
        setFormData({ ...formData, addresses: newAddresses });
    };

    const addAddress = () => {
        setFormData({ ...formData, addresses: [...formData.addresses, ""] });
    };

    const removeAddress = (index) => {
        if (formData.addresses.length > 1) {
            const newAddresses = formData.addresses.filter((_, i) => i !== index);
            setFormData({ ...formData, addresses: newAddresses });
        }
    };

    const handleSubmit = (e) => {
        e.preventDefault();
        alert("Business account created successfully!");
        console.log(formData);
    };

    return (
        <form className="account-form" onSubmit={handleSubmit}>
            <input type="text" name="companyName" placeholder="Business Name" value={formData.companyName} onChange={handleChange} required />
            <textarea name="description" placeholder="Business Description" value={formData.description} onChange={handleChange} className="textarea-input" required />
            <input type="tel" name="phone" placeholder="Business Phone" value={formData.phone} onChange={handleChange} required />
            {formData.addresses.map((address, index) => (
                <div key={index} className="address-field">
                    <input
                        type="text"
                        placeholder={`Business Address ${index + 1}`}
                        value={address}
                        onChange={(e) => handleAddressChange(index, e.target.value)}
                        required
                    />
                    {formData.addresses.length > 1 && (
                        <button type="button" className="remove-address-btn" onClick={() => removeAddress(index)}>
                            Remove
                        </button>
                    )}
                </div>
            ))}
            <button type="button" className="add-address-btn" onClick={addAddress}>
                Add Another Address
            </button>
            <div className="file-input-wrapper">
                <label className="file-label">Upload Business Photo</label>
                <input type="file" name="photo" accept="image/*" onChange={handleChange} className="file-input" />
            </div>
            <input type="text" name="requisites" placeholder="Payment Requisites" value={formData.requisites} onChange={handleChange} />
            <select name="paymentMethod" value={formData.paymentMethod} onChange={handleChange}>
                <option value="Credit Card">💳 Credit Card</option>
                <option value="Bank Transfer">🏦 Bank Transfer</option>
                <option value="PayPal">💰 PayPal</option>
            </select>
            <button type="submit">Create Business Account</button>
        </form>
    );
};

export default BusinessForm;