import React from "react";

export default function StatusBadge({ status, StatusIcon }) {
    return (
        <div
            className="status-badge"
            style={{
                background: status.color + "22",
                color: status.color
            }}
        >
            <StatusIcon size={16} />
            {status.label}
        </div>
    );
}