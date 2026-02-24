// src/pages/components/DeleteConfirm.jsx
import React from "react";

const DeleteConfirm = ({ toDelete, setShowDeleteConfirm, handleDelete }) => {
    return (
        <div className="bh-confirm">
            <div className="bh-confirm-card">
                <h4>Підтвердіть видалення</h4>
                <p>Ви видаляєте «{toDelete.name}». Це незворотно.</p>
                <div className="confirm-actions">
                    <button className="btn ghost" onClick={() => setShowDeleteConfirm(false)}>Скасувати</button>
                    <button className="btn danger" onClick={() => handleDelete(toDelete.id)}>Видалити</button>
                </div>
            </div>
        </div>
    );
};

export default DeleteConfirm;