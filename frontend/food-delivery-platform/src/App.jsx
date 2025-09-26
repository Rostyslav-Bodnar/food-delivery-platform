import React from "react";
import { BrowserRouter as Router, Routes, Route, Link } from "react-router-dom";
import RegisterForm from "./components/RegisterForm";

function App() {
    return (
        <Router>
            <div className="app-container">

                <main>
                    <Routes>
                        <Route path="/register" element={<RegisterForm />} />
                        <Route path="/" element={<h2 className="welcome">Welcome to Foodie Delivery 🍔</h2>} />
                    </Routes>
                </main>
            </div>
        </Router>
    );
}

export default App;
