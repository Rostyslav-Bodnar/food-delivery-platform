import React from "react";
import { Link } from "react-router-dom";
import { Star, MapPin, Zap } from "lucide-react";
import "./styles/DishCardComponent.css";

const DishCardComponent = ({ dish, isPopular = false }) => {
    return (
        <Link to={`/dish/${dish.id}`} className={`dish-card ${isPopular ? "popular-card" : ""}`}>
            <div className="image-wrapper">
                <img src={dish.image} alt={dish.name} />
                <div className="rating-badge">
                    <Star size={16} fill="gold" /> {dish.rating}
                </div>
                {dish.popular && (
                    <div className="popular-tag">
                        <Zap size={14} /> ХІТ
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
    );
};

export default DishCardComponent;
