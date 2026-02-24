// src/pages/components/TotalSection.jsx
import React from 'react';
import { motion } from 'framer-motion';
import "../styles/TotalSection.css";

const TotalSection = ({ getGrandTotal }) => {
    return (
        <motion.section initial={{ opacity: 0, y: 30 }} animate={{ opacity: 1, y: 0 }} className="checkout-section total-section">
            <div className="grand-total">
                <span>Total amount payable:</span>
                <strong className="grand-total-price">{getGrandTotal()} ₴</strong>
            </div>
            <button type="submit" className="submit-order-btn">
                Process all orders
            </button>
        </motion.section>
    );
};

export default TotalSection;