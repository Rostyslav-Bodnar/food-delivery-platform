// src/pages/components/SummaryBlock.jsx
import React from 'react';
import "../styles/SummaryBlock.css";

const SummaryBlock = ({ getRestaurantSubtotal, getDeliveryCost, getRestaurantTotal }) => {
    return (
        <div className="summary-block">
            <div className="summary-row"><span>Dishes:</span> <strong>{getRestaurantSubtotal()} ₴</strong></div>
            <div className="summary-row"><span>Delivery:</span> <strong>{getDeliveryCost()} ₴</strong></div>
            <div className="summary-row total"><span>Payable:</span> <strong className="final-price">{getRestaurantTotal()} ₴</strong></div>
        </div>
    );
};

export default SummaryBlock;