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
                <button onClick={() => onStatusChange(orderId, "delivered")}>
                    Delivered
                </button>
            )}
        </div>
    );
}