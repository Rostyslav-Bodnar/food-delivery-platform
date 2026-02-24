import React from 'react';
import { motion } from 'framer-motion';
import { ChevronLeft, ChevronRight, Star, MapPin, Zap } from 'lucide-react';
import { Link } from 'react-router-dom';

const PopularSection = ({ popularDishes, scrollPopular }) => {
    return (
        <motion.section
            className="popular-section"
            initial={{ opacity: 0 }}
            whileInView={{ opacity: 1 }}
            viewport={{ once: true }}
            transition={{ delay: 0.3 }}
        >
            <div className="section-header">
                <h2 className="gradient-title">Top Dishes of the Day</h2>
                <div className="scroll-controls">
                    <motion.button
                        whileHover={{ scale: 1.1 }}
                        whileTap={{ scale: 0.9 }}
                        onClick={() => scrollPopular('left')}
                        className="scroll-btn"
                    >
                        <ChevronLeft />
                    </motion.button>
                    <motion.button
                        whileHover={{ scale: 1.1 }}
                        whileTap={{ scale: 0.9 }}
                        onClick={() => scrollPopular('right')}
                        className="scroll-btn"
                    >
                        <ChevronRight />
                    </motion.button>
                </div>
            </div>
            <div className="popular-scroll">
                {popularDishes.map((dish, index) => (
                    <motion.div
                        key={dish.id}
                        initial={{ opacity: 0, y: 50 }}
                        whileInView={{ opacity: 1, y: 0 }}
                        viewport={{ once: true }}
                        transition={{ delay: index * 0.1 }}
                    >
                        <Link to={`/dish/${dish.id}`} className="dish-card popular-card">
                            <div className="image-wrapper">
                                <img src={dish.image} alt={dish.name} />
                                <div className="rating-badge">
                                    <Star size={16} fill="gold" /> {dish.rating}
                                </div>
                                {dish.popular && (
                                    <div className="popular-tag">
                                        <Zap size={14} /> HIT
                                    </div>
                                )}
                            </div>
                            <div className="dish-info">
                                <h3>{dish.name}</h3>
                                <p className="restaurant">
                                    <MapPin size={14} /> {dish.restaurant}
                                </p>
                                <div className="price-tag">{dish.price} ₴</div>
                            </div>
                        </Link>
                    </motion.div>
                ))}
            </div>
        </motion.section>
    );
};

export default PopularSection;