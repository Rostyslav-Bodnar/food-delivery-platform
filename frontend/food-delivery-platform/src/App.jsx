import React from "react";
import "./App.css";
import LoginForm from "./components/LoginForm";

function App() {
    return (
        <div className="page-wrapper">
            <div className="register-container">
                <h2>🍔 FoodExpress Login</h2>
                <LoginForm />
            </div>
        </div>
    );
}

export default App;
