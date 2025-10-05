import React from 'react';
import { BrowserRouter as Router, Routes, Route } from 'react-router-dom';
import RegisterForm from './pages/RegisterForm';
import LoginForm from './pages/LoginForm';
import Header from './components/HeaderComponent.jsx';
import HomePage from './pages/HomePage.jsx';
import Profile from './pages/ProfilePage.jsx';
import ProtectedRoute from './utils/ProtectedRoute.jsx';
import CreateAccountPage from './pages/CreateAccountPage.jsx';

function App() {
    return (
        <Router basename="/food-delivery-platform">
            <div className="app-container">
                <Header />
                <main>
                    <Routes>
                        <Route path="/" element={<HomePage />} />
                        <Route path="/login" element={<LoginForm />} />
                        <Route path="/register" element={<RegisterForm />} />
                        <Route path="/profile" element={<Profile />} />
                        <Route path="/account/create" element={<CreateAccountPage />} />
                        <Route path="/dashboard" element={
                            <ProtectedRoute>
                            </ProtectedRoute>
                        } />
                    </Routes>
                </main>
            </div>
        </Router>
    );
}

export default App;