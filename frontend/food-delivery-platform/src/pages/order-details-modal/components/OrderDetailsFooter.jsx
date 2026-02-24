import React from "react";

export default function OrderDetailsFooter({ total }) {
    return (
        <footer className="od-footer">
            <span>Total</span>
            <strong>{total} ₴</strong>
        </footer>
    );
}
