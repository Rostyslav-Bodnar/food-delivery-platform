import ErrorPanel from "../error/ErrorPanel";
import "./ToastContainer.css";

export default function ToastContainer({ toasts, removeToast }) {
    return (
        <div className="toast-container">
            {toasts.map(t => (
                <ErrorPanel
                    key={t.id}
                    message={t.message}
                    onClose={() => removeToast(t.id)}
                    autoHideMs={t.autoHideMs}
                />
            ))}
        </div>
    );
}