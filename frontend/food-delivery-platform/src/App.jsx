import React from 'react';
import { BrowserRouter as Router, Routes, Route } from 'react-router-dom';
import RegisterForm from './components/RegisterForm';
import LoginForm from './components/LoginForm';
import Header from './components/HeaderComponent.jsx';
import HomePage from './components/HomePage.jsx';
import UserPage from './components/UserPage.jsx';

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
                        <Route path="/userinfo" element={<UserPage />} />
                    </Routes>
                </main>
            </div>
        </Router>
    );
}

export default App;