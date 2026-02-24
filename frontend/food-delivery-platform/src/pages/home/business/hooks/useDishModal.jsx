// src/hooks/useDishModal.js
import { useState } from "react";

const useDishModal = () => {
    const [isModalOpen, setIsModalOpen] = useState(false);
    const [editing, setEditing] = useState(null);

    const openCreate = () => {
        setEditing(null);
        setIsModalOpen(true);
    };

    const openEdit = (dish) => {
        setEditing(dish);
        setIsModalOpen(true);
    };

    return { isModalOpen, setIsModalOpen, editing, openCreate, openEdit };
};

export default useDishModal;