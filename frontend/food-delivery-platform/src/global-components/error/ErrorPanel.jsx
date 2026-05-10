import { useEffect, useRef, useState } from "react";
import "./ErrorPanel.css";

export default function ErrorPanel({ message, onClose, autoHideMs = 4000 }) {
    const [visible, setVisible] = useState(true);
    const [exiting, setExiting] = useState(false);

    const timerRef = useRef(null);

    const clearTimer = () => {
        if (timerRef.current) clearTimeout(timerRef.current);
    };

    const startTimer = () => {
        timerRef.current = setTimeout(() => {
            handleClose();
        }, autoHideMs);
    };

    const handleClose = () => {
        setExiting(true);

        // даємо час анімації exit
        setTimeout(() => {
            setVisible(false);
            onClose?.();
        }, 220);
    };

    useEffect(() => {
        startTimer();
        return clearTimer;
    }, []);

    if (!visible) return null;

    return (
        <div
            className={`error-toast ${exiting ? "exit" : ""}`}
            onMouseEnter={clearTimer}
            onMouseLeave={startTimer}
        >
            <div className="error-card">
                <button className="error-close" onClick={handleClose}>
                    ✕
                </button>

                <div className="error-icon">⚠️</div>

                <div className="error-content">
                    <h3>Something went wrong</h3>
                    <p>{message}</p>
                </div>
            </div>
        </div>
    );
}