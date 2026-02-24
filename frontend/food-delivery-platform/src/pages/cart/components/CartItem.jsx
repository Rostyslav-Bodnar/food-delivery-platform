import { motion } from 'framer-motion';
import { Minus, Plus, Trash2 } from 'lucide-react';
import { useCart } from "../hooks/useCart.jsx";

const CartItem = ({ item }) => {
    const { updateQuantity, removeItem } = useCart();

    return (
        <motion.div key={item.id} layout className="cart-item-card">
            <img src={item.image} alt={item.name} className="cart-item-image" />
            <div className="cart-item-info">
                <h3>{item.name}</h3>
                <p className="cart-restaurant">{item.restaurant}</p>
                <div className="quantity-controls">
                    <button onClick={() => updateQuantity(item.id, item.quantity - 1)}><Minus size={16} /></button>
                    <span className="quantity">{item.quantity}</span>
                    <button onClick={() => updateQuantity(item.id, item.quantity + 1)}><Plus size={16} /></button>
                </div>
            </div>
            <div className="cart-item-price">
                <p>{item.price * item.quantity} ₴</p>
                <button onClick={() => removeItem(item.id)} className="remove-btn"><Trash2 size={18} /></button>
            </div>
        </motion.div>
    );
};

export default CartItem;