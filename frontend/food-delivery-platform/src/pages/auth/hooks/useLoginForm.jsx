import { useState } from "react";
import { login } from "../../../api/Auth.jsx";

const useLoginForm = () => {
    const [formData, setFormData] = useState({
        email: "",
        password: "",
    });

    const [error, setError] = useState(null);

    const handleChange = (e) => {
        setFormData({
            ...formData,
            [e.target.name]: e.target.value,
        });
    };

    const handleSubmit = async (e) => {
        e.preventDefault();
        setError(null);

        try {
            await login(formData);
            window.location.href = "/food-delivery-platform/profile";
        } catch {
            setError("Invalid email or password");
        }
    };

    return {
        formData,
        error,
        handleChange,
        handleSubmit,
    };
};

export default useLoginForm;