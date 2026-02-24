// src/hooks/useDeleteConfirm.js
import { useState } from "react";

const useDeleteConfirm = () => {
    const [toDelete, setToDelete] = useState(null);
    const [showDeleteConfirm, setShowDeleteConfirm] = useState(false);

    return { showDeleteConfirm, toDelete, setShowDeleteConfirm, setToDelete };
};

export default useDeleteConfirm;