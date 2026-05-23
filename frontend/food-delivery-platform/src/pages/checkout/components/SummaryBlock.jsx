// src/pages/components/SummaryBlock.jsx
import React from 'react';
import "../styles/SummaryBlock.css";

const formatFee = (value) =>
    value == null ? "—" : `${value} ₴`;

const SummaryBlock = ({ getRestaurantSubtotal, getDeliveryCost, getRestaurantTotal }) => {
    const deliveryCost = getDeliveryCost();

    return (
        <div className="summary-block">
            <div className="summary-row"><span>Dishes:</span> <strong>{getRestaurantSubtotal()} ₴</strong></div>
            <div className="summary-row"><span>Delivery:</span> <strong>{formatFee(deliveryCost)}</strong></div>
            <div className="summary-row total"><span>Payable:</span> <strong className="final-price">{getRestaurantTotal()} ₴</strong></div>
        </div>
    );
};

export default SummaryBlock;
