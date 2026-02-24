// src/pages/components/OrderItem.jsx
import React from 'react';
import { motion } from 'framer-motion';
import { Trash2 } from 'lucide-react';
import "../styles/OrderItem.css";

const OrderItem = ({ item, removeItem }) => {
    return (
        <motion.div layout initial={{ opacity: 0 }} animate={{ opacity: 1 }} exit={{ opacity: 0, x: -50 }} className="order-item">
            <img src={item.image} alt={item.name} />
            <div className="order-item-info">
                <p className="name">{item.name}</p>
                <p className="quantity">Quantity: {item.quantity}</p>
            </div>
            <div className="order-item-price">
                <span>{item.price * item.quantity} ₴</span>
                <button type="button" onClick={() => removeItem(item.id)} className="remove-btn">
                    <Trash2 size={20} />
                </button>
            </div>
        </motion.div>
    );
};

export default OrderItem;