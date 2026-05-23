import {
    useEffect,
    useRef,
    useState
} from "react";

import {
    AlertTriangle,
    CheckCircle2,
    Info,
    XCircle
} from "lucide-react";

import "./ErrorPanel.css";

const TOAST_VARIANTS = {
    error: {
        defaultTitle: "Something went wrong",
        Icon: XCircle
    },
    success: {
        defaultTitle: "Success",
        Icon: CheckCircle2
    },
    info: {
        defaultTitle: "Heads up",
        Icon: Info
    },
    warning: {
        defaultTitle: "Warning",
        Icon: AlertTriangle
    }
};

export default function ErrorPanel({
                                       message,
                                       onClose,
                                       autoHideMs = 4000,
                                       type = "error",
                                       title
                                   }) {
    const [visible, setVisible] = useState(true);
    const [exiting, setExiting] = useState(false);

    const timerRef = useRef(null);

    const clearTimer = () => {
        if (timerRef.current) {
            clearTimeout(timerRef.current);
        }
    };

    const handleClose = () => {
        setExiting(true);

        setTimeout(() => {
            setVisible(false);
            onClose?.();
        }, 220);
    };

    const startTimer = () => {
        timerRef.current = setTimeout(() => {
            handleClose();
        }, autoHideMs);
    };

    useEffect(() => {
        startTimer();
        return clearTimer;
    }, []);

    if (!visible) {
        return null;
    }

    const variant = TOAST_VARIANTS[type] ?? TOAST_VARIANTS.error;
    const Icon = variant.Icon;
    const resolvedTitle = title === null
        ? null
        : title ?? variant.defaultTitle;

    return (
        <div
            className={`app-toast is-${type} ${exiting ? "exit" : ""}`}
            onMouseEnter={clearTimer}
            onMouseLeave={startTimer}
            role="status"
            aria-live="polite"
        >
            <div className="app-toast__card">
                <button
                    className="app-toast__close"
                    onClick={handleClose}
                    aria-label="Close notification"
                >
                    <XCircle size={16} />
                </button>

                <div className="app-toast__icon">
                    <Icon size={20} strokeWidth={2.2} />
                </div>

                <div className="app-toast__content">
                    {resolvedTitle && <h3>{resolvedTitle}</h3>}
                    <p>{message}</p>
                </div>
            </div>
        </div>
    );
}
