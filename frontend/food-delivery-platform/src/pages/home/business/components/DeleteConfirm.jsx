// src/pages/components/DeleteConfirm.jsx
import React from "react";

const DeleteConfirm = ({ toDelete, setShowDeleteConfirm, handleDelete }) => {
    return (
        <div className="bh-confirm">
            <div className="bh-confirm-card">
                <h4>Confirm Deletion</h4>
                <p>You are deleting “{toDelete.name}”. This action cannot be undone.</p>
                <div className="confirm-actions">
                    <button
                        className="btn ghost"
                        onClick={() => setShowDeleteConfirm(false)}
                    >
                        Cancel
                    </button>
                    <button
                        className="btn danger"
                        onClick={() => handleDelete(toDelete.id)}
                    >
                        Delete
                    </button>
                </div>
            </div>
        </div>
    );
};

export default DeleteConfirm;