// src/AppRouter.jsx
import React from "react";
import { BrowserRouter as Router, Routes, Route } from "react-router-dom";
import Header from "./pages/HeaderComponent.jsx";
import HomePage from "./pages/home/HomePage.jsx";
import AuthorizationPage from "./pages/auth/AuthorationPage.jsx";
import ProfilePage from "./pages/profile/ProfilePage.jsx";
import CreateAccountPage from "./pages/create-account/CreateAccountPage.jsx";
import DishPage from "./pages/dish/DishPage.jsx";
import ProtectedRoute from "./utils/ProtectedRoute.jsx";
import CartPage from "./pages/cart/CartPage.jsx";
import CheckoutPage from "./pages/checkout/CheckoutPage.jsx";
import RestaurantsPage from "./pages/restaurants/RestaurantsPage.jsx";
import RestaurantDetailsPage from './pages/restaurant-details/RestaurantDetailsPage';
import CustomerOrdersPage from "./pages/customer-orders/CustomerOrdersPage.jsx";
import BusinessOrdersPage from "./pages/business-orders/BusinessOrdersPage";

const AppRouter = () => {
    return (
        <Router basename="/food-delivery-platform">
            <div className="app-container">
                <Header />
                <main>
                    <Routes>
                        <Route path="/" element={<HomePage />} />
                        <Route path="/auth" element={<AuthorizationPage />} />
                        <Route path="/dish/:id" element={<DishPage />} />
                        <Route path="/cart" element={<CartPage />} />
                        <Route path="/checkout" element={<CheckoutPage />} />
                        <Route path="/restaurants" element={<RestaurantsPage />} />
                        <Route path="/restaurant/:id" element={<RestaurantDetailsPage />} />
                        <Route path="/customer/orders" element={<CustomerOrdersPage />} />
                        <Route path="/business/orders" element={<BusinessOrdersPage />} />
                        <Route
                            path="/profile"
                            element={
                                <ProtectedRoute>
                                    <ProfilePage />
                                </ProtectedRoute>
                            }
                        />
                        <Route
                            path="/account/create"
                            element={
                                <ProtectedRoute>
                                    <CreateAccountPage />
                                </ProtectedRoute>
                            }
                        />
                        <Route
                            path="/dashboard"
                            element={
                                <ProtectedRoute>
                                    <h1>Dashboard (Protected)</h1>
                                </ProtectedRoute>
                            }
                        />
                    </Routes>
                </main>
            </div>
        </Router>
    );
};

export default AppRouter;