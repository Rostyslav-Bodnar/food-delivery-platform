// src/hooks/useCheckoutForm.js
import { useState } from 'react';

const useCheckoutForm = () => {
    const [formData, setFormData] = useState({
        name: '', phone: '', email: '', comment: ''
    });

    const handleInputChange = (e) => {
        setFormData(prev => ({ ...prev, [e.target.name]: e.target.value }));
    };

    return { formData, handleInputChange };
};

export default useCheckoutForm;