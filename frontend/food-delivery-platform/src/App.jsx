import React from 'react';
import { BrowserRouter as Router, Routes, Route, Link } from 'react-router-dom';
import RegisterForm from './components/RegisterForm';
import LoginForm from './components/LoginForm';
import Header from './components/HeaderComponent.jsx';

function App() {
    return (
        <Router basename="/food-delivery-platform">
            <div className="app-container">
                <Header />
                <main>
                    <Routes>
                        <Route path="/" element={
                            <div className="page-wrapper">
                                <h2 className="welcome">Welcome to Foodie Delivery 🍔</h2>
                                <div style={{ marginTop: '2rem' }}>
                                    <Link className="nav-link" to="/login">Login</Link>
                                    <Link className="nav-link" to="/register" style={{ marginLeft: '1rem' }}>Register</Link>
                                </div>
                            </div>
                        } />
                        <Route path="/login" element={<LoginForm />} />
                        <Route path="/register" element={<RegisterForm />} />
                    </Routes>
                </main>
            </div>
        </Router>
    );
}

export default App;