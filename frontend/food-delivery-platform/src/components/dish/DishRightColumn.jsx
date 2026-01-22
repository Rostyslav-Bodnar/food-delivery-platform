import React from "react";
import { motion } from "framer-motion";
import { Star, Clock, MapPin, Plus, Minus } from "lucide-react";
import "./styles/DishRightColumn.css";

const DishRightColumn = ({ dish, quantity, setQuantity, handleAddToCart }) => {
    return (
        <div className="dish-info">
            <h1 className="dish-title">{dish.name}</h1>

            <div className="dish-meta">
                <div className="rating"><Star size={20} fill="gold" /> {dish.rating}</div>
                <div className="cooking-time"><Clock size={18} /> {dish.cookingTime}</div>
            </div>

            <div className="restaurant-info">
                <MapPin size={16} /> <strong>{dish.restaurant}</strong><br />
            </div>

            <div className="price-section">
                <div className="price-current">{dish.price} ₴</div>
            </div>

            <p className="description">{dish.description}</p>

            {/* Кількість */}
            <div className="quantity-controls">
                <motion.button onClick={() => setQuantity(q => Math.max(1, q - 1))} className="qty-btn minus">
                    <Minus size={28} />
                </motion.button>

                <motion.span className="quantity">{quantity}</motion.span>

                <motion.button onClick={() => setQuantity(q => q + 1)} className="qty-btn plus">
                    <Plus size={28} />
                </motion.button>
            </div>

            <motion.button
                className="add-to-cart-btn"
                onClick={handleAddToCart}
            >
                Add to cart · {dish.price * quantity} ₴
            </motion.button>

            <div className="extra-info">
                <div className="info-block">
                    <h4>Ingredients</h4>
                    <ul>
                        {dish.ingredients.map((ing, i) => <li key={i}>{ing}</li>)}
                    </ul>
                </div>

                <div className="info-block">
                    <h4>Nutritional Value</h4>
                    <div className="nutrition-grid">
                        <div>Calories: <strong>{dish.nutrition.calories}</strong></div>
                        <div>Protein: <strong>{dish.nutrition.protein} g</strong></div>
                        <div>Fat: <strong>{dish.nutrition.fats} g</strong></div>
                        <div>Carbohydrates: <strong>{dish.nutrition.carbs} g</strong></div>
                    </div>
                </div>


                {dish.allergens.length > 0 && (
                    <div className="info-block warning">
                        <h4>Allergens</h4>
                        <p>{dish.allergens.join(', ')}</p>
                    </div>
                )}
            </div>
        </div>
    );
};

export default DishRightColumn;
