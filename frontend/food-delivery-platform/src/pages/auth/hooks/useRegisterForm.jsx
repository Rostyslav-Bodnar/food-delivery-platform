import { useState } from "react";
import { register } from "../../../api/Auth.jsx";

const useRegisterForm = () => {
    const [formData, setFormData] = useState({
        name: "",
        surname: "",
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
            await register(formData);
            window.location.href = "/food-delivery-platform/profile";
        } catch {
            setError("Registration failed");
        }
    };

    return {
        formData,
        error,
        handleChange,
        handleSubmit,
    };
};

export default useRegisterForm;