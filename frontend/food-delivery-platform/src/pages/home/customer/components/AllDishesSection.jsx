import React from 'react';
import { motion } from 'framer-motion';
import DishCardComponent from '../../../../global-components/DishCardComponent';

const AllDishesSection = ({ filteredDishes, loading }) => {
    return (
        <motion.section
            className="dishes-section"
            initial={{ opacity: 0 }}
            whileInView={{ opacity: 1 }}
            viewport={{ once: true }}
        >
            <h2 className="section-title">All Dishes</h2>
            {loading ? (
                <div className="skeleton-grid">
                    {[...Array(6)].map((_, i) => (
                        <motion.div
                            key={i}
                            className="skeleton-card"
                            initial={{ opacity: 0 }}
                            animate={{ opacity: 1 }}
                            transition={{ delay: i * 0.1 }}
                        />
                    ))}
                </div>
            ) : filteredDishes.length === 0 ? (
                <motion.p
                    initial={{ opacity: 0, y: 20 }}
                    animate={{ opacity: 1, y: 0 }}
                    className="no-results"
                >
                    Nothing found
                </motion.p>
            ) : (
                <motion.div className="dishes-grid">
                    {filteredDishes.map((dish, index) => (
                        <motion.div
                            key={dish.id}
                            initial={{ opacity: 0, scale: 0.9 }}
                            whileInView={{ opacity: 1, scale: 1 }}
                            viewport={{ once: true }}
                            transition={{ delay: index * 0.05 }}
                            whileHover={{ y: -8 }}
                        >
                            <DishCardComponent dish={dish} />
                        </motion.div>
                    ))}
                </motion.div>
            )}
        </motion.section>
    );
};

export default AllDishesSection;