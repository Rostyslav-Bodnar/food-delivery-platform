import React from "react";

const LoginForm = () => {
    return (
        <form className="register-form">
            <input type="text" placeholder="Username" />
            <input type="password" placeholder="Password" />
            <button type="submit">Login</button>
            <p className="login-text">
                Don't have an account? <a href="#">Register</a>
            </p>
        </form>
    );
};

export default LoginForm;
