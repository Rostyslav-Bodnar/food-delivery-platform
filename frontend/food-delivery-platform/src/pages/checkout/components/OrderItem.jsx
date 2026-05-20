// src/pages/components/OrderItem.jsx
import React from 'react';
import { motion } from 'framer-motion';
import { Trash2 } from 'lucide-react';
import "../styles/OrderItem.css";

const OrderItem = ({ item, removeItem }) => {
    return (
        <motion.div
            layout
            initial={{ opacity: 0 }}
            animate={{ opacity: 1 }}
            exit={{ opacity: 0, x: -40 }}
            className="order-item"
        >
            <button
                type="button"
                onClick={() => removeItem(item.id)}
                className="remove-btn"
            >
                <Trash2 size={14} />
            </button>

            <img src={item.image} alt={item.name} />

            <div className="order-item-main">
                <p className="name">{item.name}</p>

                <div className="order-meta">
                    <span className="price">
                        {item.price * item.quantity} ₴
                    </span>
                    <span className="quantity">x{item.quantity}</span>
                </div>
            </div>
        </motion.div>
    );
};

export default OrderItem;