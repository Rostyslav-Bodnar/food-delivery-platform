// src/pages/components/BottomLinks.jsx
import React from 'react';
import { Link } from 'react-router-dom';
import { ArrowLeft } from 'lucide-react';

const BottomLinks = () => {
    return (
        <div className="bottom-links">
            <Link to="/" className="continue-shopping-btn">
                Continue shopping
            </Link>
            <Link to="/cart" className="back-to-cart">
                <ArrowLeft size={20} /> Back to cart
            </Link>
        </div>
    );
};

export default BottomLinks;