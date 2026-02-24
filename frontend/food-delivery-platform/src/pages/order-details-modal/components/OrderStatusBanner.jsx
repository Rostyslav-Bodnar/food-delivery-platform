import React from "react";

export default function OrderStatusBanner({ status }) {
    const StatusIcon = status.icon;

    return (
        <div
            className="od-status"
            style={{
                background: `linear-gradient(90deg, ${status.color}22, ${status.color}11)`,
                color: status.color
            }}
        >
            <StatusIcon size={18} />
            {status.label}
        </div>
    );
}