// src/pages/components/ContactInfo.jsx
import React from 'react';
import { motion } from 'framer-motion';
import { User } from 'lucide-react';
import "../styles/ContactInfo.css";

const ContactInfo = ({ formData, handleInputChange }) => {
    return (
        <motion.section
            initial={{ opacity: 0, y: 20 }}
            animate={{ opacity: 1, y: 0 }}
            className="checkout-section"
        >
            <h2><User size={28} /> Contact information</h2>
            <div className="form-grid">
                <input
                    type="text"
                    name="name"
                    placeholder="Your name *"
                    required
                    value={formData.name}
                    onChange={handleInputChange}
                />
                <input
                    type="tel"
                    name="phone"
                    placeholder="Phone number *"
                    required
                    value={formData.phone}
                    onChange={handleInputChange}
                />
                <input
                    type="email"
                    name="email"
                    placeholder="Email (optional)"
                    value={formData.email}
                    onChange={handleInputChange}
                />
            </div>
        </motion.section>
    );
};

export default ContactInfo;