import React from "react";
import { BrowserRouter as Router, Routes, Route } from "react-router-dom";
import Header from "./components/HeaderComponent.jsx";
import HomePage from "./pages/HomePage.jsx";
import LoginForm from "./pages/LoginForm.jsx";
import RegisterForm from "./pages/RegisterForm.jsx";
import ProfilePage from "./pages/ProfilePage.jsx";
import CreateAccountPage from "./pages/CreateAccountPage.jsx";
import ProtectedRoute from "./utils/ProtectedRoute.jsx";

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
                        {/* Приклад майбутньої захищеної сторінки */}
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
