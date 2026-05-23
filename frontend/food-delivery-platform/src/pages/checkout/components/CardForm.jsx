// src/pages/components/CardForm.jsx
import React from 'react';
import { motion } from 'framer-motion';
import "../styles/CardForm.css"

const CardForm = ({ settings, handleCardChange }) => {
    return (
        <motion.div initial={{ opacity: 0 }} animate={{ opacity: 1 }} className="card-form">
            <input type="text" name="cardNumber" placeholder="Card number" value={settings.cardData.cardNumber} onChange={handleCardChange} required />
            <div className="card-row">
                <input type="text" name="cardExpiry" placeholder="MM/YY" value={settings.cardData.cardExpiry} onChange={handleCardChange} required />
                <input type="text" name="cardCVV" placeholder="CVV" value={settings.cardData.cardCVV} onChange={handleCardChange} required />
            </div>
            <input type="text" name="cardName" placeholder="Name on the card" value={settings.cardData.cardName} onChange={handleCardChange} required />
        </motion.div>
    );
};

export default CardForm;