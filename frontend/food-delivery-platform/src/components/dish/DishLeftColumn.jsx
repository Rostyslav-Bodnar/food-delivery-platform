import React from "react";
import { motion } from "framer-motion";
import { Star, Zap, MessageCircle } from "lucide-react";
import "./styles/DishLeftColumn.css";

const DishLeftColumn = ({ dish, reviewsList }) => {
    return (
        <div className="dish-left">
            <div className="dish-image-wrapper">
                <img
                    src={dish.image}
                    alt={dish.name}
                    className="dish-image"
                />
                {dish.popular && (
                    <div className="popular-badge">
                        <Zap size={16} /> ХІТ
                    </div>
                )}
            </div>

            {/* Відгуки (МОК) */}
            <div className="reviews-section">
                <h3 className="reviews-title">
                    <MessageCircle size={20} /> Reviews ({reviewsList.length})
                </h3>

                <div className="reviews-list">
                    {reviewsList.map((r) => (
                        <motion.div
                            key={r.id}
                            className="review-card"
                            initial={{ opacity: 0, x: -20 }}
                            animate={{ opacity: 1, x: 0 }}
                        >
                            <div className="review-header">
                                <strong>{r.author}</strong>
                                <div className="review-rating">
                                    {[...Array(5)].map((_, i) => (
                                        <Star
                                            key={i}
                                            size={14}
                                            fill={i < r.rating ? "gold" : "none"}
                                            stroke={i < r.rating ? "gold" : "#555"}
                                        />
                                    ))}
                                </div>
                            </div>

                            <p className="review-text">{r.text}</p>
                            <small className="review-date">{r.date}</small>
                        </motion.div>
                    ))}
                </div>

                <button className="show-more-reviews">
                    Show all reviews →
                </button>
            </div>
        </div>
    );
};

export default DishLeftColumn;
