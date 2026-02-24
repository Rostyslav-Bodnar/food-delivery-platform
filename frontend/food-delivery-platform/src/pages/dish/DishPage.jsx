import { useParams, Link } from 'react-router-dom';
import { motion } from 'framer-motion';
import { ChevronLeft } from 'lucide-react';
import './styles/DishPage.css';

import DishLeftColumn from "./components/DishLeftColumn";
import DishRightColumn from "./components/DishRightColumn";

import { useDish } from "./hooks/useDish";
import { useDishQuantity } from "./hooks/useDishQuantity";
import { useAddDishToCart } from "./hooks/useAddDishToCart";

const DishPage = () => {
    const { id } = useParams();

    const { dish, loading } = useDish(id);
    const { quantity, setQuantity } = useDishQuantity();
    const { addDish } = useAddDishToCart();

    if (loading) {
        return (
            <div className="dish-loading">
                <div className="spinner"></div>
                Loading...
            </div>
        );
    }

    if (!dish) {
        return (
            <div className="dish-not-found">
                <h2>Dish not found</h2>
                <Link to="/">Go to home</Link>
            </div>
        );
    }

    const handleAddToCart = () => {
        addDish(dish, quantity);
    };

    //MOCK DATA
    const reviewsList = [
        {
            id: 1,
            author: "Alex",
            rating: 5,
            text: "Very tasty!",
            date: "2024-02-10"
        }
    ];


    return (
        <div className="dish-page">
            <motion.div
                initial={{ opacity: 0, y: 20 }}
                animate={{ opacity: 1, y: 0 }}
                className="dish-container"
            >
                <div className="breadcrumbs">
                    <Link to="/">Home</Link> →
                    <span>{dish.category}</span> → {dish.name}
                </div>

                <div className="dish-grid">
                    <DishLeftColumn dish={dish} reviewsList={reviewsList} />
                    <DishRightColumn
                        dish={dish}
                        quantity={quantity}
                        setQuantity={setQuantity}
                        handleAddToCart={handleAddToCart}
                    />
                </div>

                <Link to="/" className="back-to-menu">
                    <ChevronLeft size={18} /> Back to home
                </Link>
            </motion.div>
        </div>
    );
};

export default DishPage;
