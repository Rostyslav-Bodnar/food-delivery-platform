import React from "react";

export default function StatusActions({
                                          status,
                                          orderId,
                                          onStatusChange
                                      }) {
    return (
        <div className="status-actions">
            {status === "pending" && (
                <>
                    <button onClick={() => onStatusChange(orderId, "preparing")}>
                        Accept
                    </button>
                    <button
                        className="danger"
                        onClick={() => onStatusChange(orderId, "cancelled")}
                    >
                        Cancel
                    </button>
                </>
            )}

            {status === "preparing" && (
                <button onClick={() => onStatusChange(orderId, "ready")}>
                    Ready
                </button>
            )}

            {status === "ready" && (
                <span className="status-actions__note">
                    Waiting for courier
                </span>
            )}

            {status === "on-the-way" && (
                <button onClick={() => onStatusChange(orderId, "picked-up")}>
                    Picked up
                </button>
            )}

            {status === "picked-up" && (
                <span className="status-actions__note">
                    Courier to customer
                </span>
            )}
        </div>
    );
}
