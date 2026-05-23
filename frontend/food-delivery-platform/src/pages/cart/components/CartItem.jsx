import { motion } from 'framer-motion';
import { Minus, Plus, Trash2 } from 'lucide-react';

const CartItem = ({ item, updateQuantity, removeItem }) => {
    return (
        <motion.div
            layout
            initial={{ opacity: 0, y: 8 }}
            animate={{ opacity: 1, y: 0 }}
            exit={{ opacity: 0, scale: 0.95 }}
            className="cart-item-card"
        >
            <img src={item.image} alt={item.name} className="cart-item-image" />
            <div className="cart-item-info">
                <h3>{item.name}</h3>
                <p className="cart-restaurant">{item.restaurant}</p>
                <div className="quantity-controls">
                    <button
                        type="button"
                        onClick={() => updateQuantity(item.id, item.quantity - 1)}
                        disabled={item.quantity <= 1}
                        aria-label="Decrease quantity"
                    >
                        <Minus size={18} strokeWidth={2.5} />
                    </button>
                    <span className="quantity">{item.quantity}</span>
                    <button
                        type="button"
                        onClick={() => updateQuantity(item.id, item.quantity + 1)}
                        aria-label="Increase quantity"
                    >
                        <Plus size={18} strokeWidth={2.5} />
                    </button>
                </div>
            </div>
            <div className="cart-item-price">
                <p>{item.price * item.quantity} ₴</p>
                <button
                    type="button"
                    onClick={() => removeItem(item.id)}
                    className="remove-btn"
                    aria-label="Remove item"
                >
                    <Trash2 size={18} strokeWidth={2.25} />
                </button>
            </div>
        </motion.div>
    );
};

export default CartItem;
