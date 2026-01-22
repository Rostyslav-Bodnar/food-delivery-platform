// src/AppRouter.jsx
import React from "react";
import { BrowserRouter as Router, Routes, Route } from "react-router-dom";
import Header from "./components/HeaderComponent.jsx";
import HomePage from "./pages/HomePage.jsx";
import AuthorizationPage from "./pages/AuthorationPage.jsx";
import ProfilePage from "./pages/ProfilePage.jsx";
import CreateAccountPage from "./pages/CreateAccountPage.jsx";
import DishPage from "./pages/DishPage.jsx";
import ProtectedRoute from "./utils/ProtectedRoute.jsx";
import CartPage from "./pages/CartPage.jsx";
import CheckoutPage from "./pages/CheckoutPage.jsx";
import RestaurantsPage from "./pages/RestaurantsPage.jsx";
import RestaurantDetailsPage from './pages/RestaurantDetailsPage';
import CustomerOrdersPage from "./pages/CustomerOrdersPage.jsx";
import BusinessOrdersPage from "./pages/BusinessOrdersPage";

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