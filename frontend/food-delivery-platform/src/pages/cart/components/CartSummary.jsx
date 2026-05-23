import { motion } from 'framer-motion';
import { Link } from 'react-router-dom';

const CartSummary = ({ totalPrice }) => {
    return (
        <motion.div className="cart-summary">
            <div className="summary-row total">
                <span>Total:</span>
                <strong className="final-price">{totalPrice} ₴</strong>
            </div>

            <Link to="/checkout" className="checkout-btn">
                Proceed to Checkout
            </Link>

            <Link to="/" className="continue-shopping">
                Continue Shopping
            </Link>
        </motion.div>
    );
};

export default CartSummary;