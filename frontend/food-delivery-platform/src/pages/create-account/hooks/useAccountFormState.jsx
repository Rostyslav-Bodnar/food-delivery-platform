import { useState } from "react";

export const useAccountFormState = (initialState) => {
    const [formData, setFormData] = useState(initialState);

    const changeField = (e) => {
        const { name, value } = e.target;
        setFormData(prev => ({
            ...prev,
            [name]: value
        }));
    };

    return {
        formData,
        setFormData,
        changeField
    };
};
