import { Link } from "react-router-dom";
import { motion } from "framer-motion";
import { ChevronRight, Star, Clock, MapPin } from "lucide-react";
import "./styles/RestaurantCard.css";

const RestaurantCard = ({ restaurant }) => {
    return (
        <motion.div
            layout
            initial={{ opacity: 0, scale: 0.9 }}
            animate={{ opacity: 1, scale: 1 }}
            exit={{ opacity: 0, scale: 0.9 }}
            transition={{ duration: 0.2 }}
            whileHover={{ y: -12, scale: 1.03 }}
            className="restaurant-card"
        >
            <Link
                to={`/restaurant/${restaurant.id}`}
                className="restaurant-link"
                state={{ restaurant }}
            >
                <div className="restaurant-image-wrapper">
                    <img
                        src={restaurant.image || "https://via.placeholder.com/400x250?text=No+Image"}
                        alt={restaurant.name}
                    />
                    <div className="restaurant-overlay">
                        <ChevronRight size={30} />
                    </div>
                </div>

                <div className="restaurant-info">
                    <div className="restaurant-header">
                        <h3>{restaurant.name}</h3>
                        <div className="rating-badge">
                            <Star size={16} fill="gold" /> {restaurant.rating}
                        </div>
                    </div>

                    <p className="restaurant-category">
                        {restaurant.category}
                    </p>

                    <div className="restaurant-details">
                        <span><Clock size={15} /> {restaurant.deliveryTime}</span>
                        <span><MapPin size={15} /> {restaurant.deliveryPrice}</span>
                    </div>
                </div>
            </Link>
        </motion.div>
    );
};

export default RestaurantCard;
