import React from "react";
import { Link, useLocation } from "react-router-dom";
import { Home, ShoppingCart, Store, User, Package } from "lucide-react";
import "../styles/CustomerHomePage.css"; // щоб стилі sidebar були доступні

const CustomerSidebar = () => {
    const location = useLocation();

    const isActive = (path) => location.pathname === path;

    return (
        <aside className="customer-sidebar">
            <div className="sidebar-logo">FoodEx</div>

            <nav className="sidebar-nav">
                <Link to="/" className={`sidebar-item ${isActive("/") ? "active" : ""}`}>
                    <Home size={22} /> <span>Головна</span>
                </Link>

                <Link to="/cart" className={`sidebar-item ${isActive("/cart") ? "active" : ""}`}>
                    <ShoppingCart size={22} /> <span>Кошик</span>
                </Link>

                <Link to="/restaurants" className={`sidebar-item ${isActive("/restaurants") ? "active" : ""}`}>
                    <Store size={22} /> <span>Заклади</span>
                </Link>

                <Link to="/customer/orders" className={`sidebar-item ${isActive("/customer/orders") ? "active" : ""}`}>
                    <Package size={22} /> <span>Замовлення</span>
                </Link>

                <Link to="/profile" className={`sidebar-item ${isActive("/profile") ? "active" : ""}`}>
                    <User size={22} /> <span>Профіль</span>
                </Link>
            </nav>
        </aside>
    );
};

export default CustomerSidebar;
