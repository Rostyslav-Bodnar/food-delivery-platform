import React from "react";

export default function OrderDetailsFooter({ total }) {
    return (
        <footer className="od-footer">
            <span>Total</span>
            <strong>{Number(total ?? 0).toFixed(2)} ₴</strong>
        </footer>
    );
}
