// src/pages/components/CommentSection.jsx
import React from 'react';
import { motion } from 'framer-motion';
import "../styles/CommentSection.css";

const CommentSection = ({ formData, handleInputChange }) => {
    return (
        <motion.section
            initial={{ opacity: 0, y: 20 }}
            animate={{ opacity: 1, y: 0 }}
            className="checkout-section"
        >
            <h2>Order comment (general)</h2>
            <textarea
                name="comment"
                placeholder="Additional notes or requests"
                rows="5"
                value={formData.comment}
                onChange={handleInputChange}
            />
        </motion.section>
    );
};

export default CommentSection;