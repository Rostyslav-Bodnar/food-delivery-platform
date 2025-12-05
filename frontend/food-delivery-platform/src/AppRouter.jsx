// src/AppRouter.jsx
import React from "react";
import { BrowserRouter as Router, Routes, Route } from "react-router-dom";
import Header from "./components/HeaderComponent.jsx";
import HomePage from "./pages/HomePage.jsx";
import LoginForm from "./pages/LoginForm.jsx";
import RegisterForm from "./pages/RegisterForm.jsx";
import ProfilePage from "./pages/ProfilePage.jsx";
import CreateAccountPage from "./pages/CreateAccountPage.jsx";
import DishPage from "./pages/DishPage.jsx";
import ProtectedRoute from "./utils/ProtectedRoute.jsx";
import CartPage from "./pages/CartPage.jsx";
import RestaurantsPage from "./pages/RestaurantsPage.jsx";
import RestaurantDetailsPage from './pages/RestaurantDetailsPage';
import CustomerOrdersPage from "./pages/CustomerOrdersPage.jsx";
import BusinessOrdersPage from "./pages/BusinessOrdersPage";
import CourierHomePage from "./components/curier/CourierHomePage.jsx";


const AppRouter = () => {
    return (
        <Router basename="/food-delivery-platform">
            <div className="app-container">
                <Header />
                <main>
                    <Routes>
                        <Route path="/" element={<HomePage />} />
                        <Route path="/login" element={<LoginForm />} />
                        <Route path="/register" element={<RegisterForm />} />
                        <Route path="/dish/:id" element={<DishPage />} />
                        <Route path="/cart" element={<CartPage />} />
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

                        {/* НОВИЙ МАРШРУТ ДЛЯ КУР’ЄРА */}
                        <Route
                            path="/courier"
                            element={
                                <ProtectedRoute>
                                    <CourierHomePage />
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