import React, { useEffect, useMemo } from "react";
import { X } from "lucide-react";
import { motion, AnimatePresence } from "framer-motion";
import { loadStripe } from "@stripe/stripe-js";
import { Elements, PaymentElement, useStripe, useElements } from "@stripe/react-stripe-js";

const stripePromise = loadStripe(
    import.meta.env.VITE_STRIPE_PUBLISHABLE_KEY ||
    "pk_test_51SeMlmL2Z4y20S3ELazXa1alP4LwhOeL91Jd54mZcEE4f0e5e7pKRnpMLdFh2uNNVbUhEyC8JF7jjKfyAneWhAPT00bgasYuIv"
);

function useLockBodyScroll(isLocked) {
    useEffect(() => {
        if (!isLocked) return;
        const original = document.body.style.overflow;
        document.body.style.overflow = "hidden";
        return () => { document.body.style.overflow = original; };
    }, [isLocked]);
}

const ConfirmButton = ({ label = "Pay", onSuccess }) => {
    const stripe = useStripe();
    const elements = useElements();
    const [loading, setLoading] = React.useState(false);

    const handlePay = async (e) => {
        e.preventDefault();
        if (!stripe || !elements) return;
        setLoading(true);
        const { error } = await stripe.confirmPayment({
            elements,
            redirect: "if_required",
        });
        setLoading(false);
        if (error) {
            alert(error.message || "Your payment was declined. Please try a different card or payment method.");
        } else {
            onSuccess?.();
        }
    };

    return (
        <button className="spm-btn spm-btn-primary" onClick={handlePay} disabled={!stripe || loading}>
            {loading ? "Processing..." : label}
        </button>
    );
};

const StripePaymentModal = ({
                                open,
                                onClose,
                                clientSecret,
                                title = "Payment by card",
                                subtitle,
                                onPaid
                            }) => {
    useLockBodyScroll(open);

    // ESC → закрити
    useEffect(() => {
        if (!open) return;
        const onKey = (e) => { if (e.key === "Escape") onClose?.(); };
        window.addEventListener("keydown", onKey);
        return () => window.removeEventListener("keydown", onKey);
    }, [open, onClose]);

    const elementsOptions = useMemo(() => {
        if (!clientSecret) return null;

        const appearance = {
            theme: "night",
            variables: {
                colorPrimary: "#7c5cff",
                colorText: "#e6eef6",
                colorBackground: "transparent",
                colorDanger: "#ff6b6b",
                borderRadius: "12px",           // ← зменшено для компактності
                spacingUnit: "8px",             // ← головна зміна для меншої висоти
                fontFamily:
                    "Inter, system-ui, -apple-system, Segoe UI, Roboto, 'Helvetica Neue', Arial, 'Noto Sans', 'Apple Color Emoji', 'Segoe UI Emoji'",
            },
            rules: {
                ".AccordionItem": {
                  padding: "20px"  
                },
                ".Input": {
                    backgroundColor: "rgba(255,255,255,0.06)",
                    color: "#e6eef6",
                    border: "1px solid rgba(255,255,255,0.12)",
                    padding: "6px 14px",        // ← було 12px — тепер висота поля менша
                },
                ".Input:focus": {
                    border: "1px solid #7c5cff",
                    boxShadow: "0 0 0 3px rgba(124,92,255,0.2)",
                },
                ".Label": {
                    color: "rgba(230,238,246,0.85)",
                    fontWeight: "600",
                },
                ".Tab, .Pill": {
                    backgroundColor: "rgba(255,255,255,0.06)",
                    border: "1px solid rgba(255,255,255,0.12)",
                    color: "#e6eef6",
                },
                ".Tab:hover, .Pill:hover": {
                    backgroundColor: "rgba(124,92,255,0.15)",
                    border: "1px solid #7c5cff",
                },
                ".Error": { color: "#ff6b6b" },
                ".HelpText": { color: "rgba(230,238,246,0.7)" },
                ".Link": { color: "#9aa9ff" },
            },
            labels: "floating",
        };

        return { clientSecret, appearance, locale: "uk", loader: "auto" };
    }, [clientSecret]);

    // Закриваємо тільки при реальному кліку по підкладці
    const handleOverlayMouseDown = (e) => {
        if (e.target === e.currentTarget) {
            onClose?.();
        }
    };

    return (
        <AnimatePresence>
            {open && clientSecret && (
                <motion.div
                    className="spm-overlay"
                    initial={{ opacity: 0 }}
                    animate={{ opacity: 1 }}
                    exit={{ opacity: 0 }}
                    onMouseDown={handleOverlayMouseDown}
                >
                    <motion.div
                        className="spm-modal"
                        initial={{ y: 30, opacity: 0, scale: 0.98 }}
                        animate={{ y: 0, opacity: 1, scale: 1 }}
                        exit={{ y: 20, opacity: 0, scale: 0.98 }}
                        transition={{ type: "spring", stiffness: 260, damping: 22 }}
                        role="dialog"
                        aria-modal="true"
                        aria-labelledby="spm-title"
                    >
                        <button className="spm-close" onClick={onClose} aria-label="Close">
                            <X size={20} />
                        </button>

                        <div className="spm-header">
                            <h3 id="spm-title">{title}</h3>
                            {subtitle && <p className="spm-subtitle">{subtitle}</p>}
                        </div>

                        {/* Прокручуваний вміст */}
                        <div className="spm-scroll">
                            <Elements stripe={stripePromise} options={elementsOptions}>
                                <form onSubmit={(e) => e.preventDefault()}>
                                    {/* ← Ось і вся зміна: обгортка для вужчих полів */}
                                    <div className="spm-payment-container">
                                        <PaymentElement />
                                    </div>

                                    {/* Sticky панель дій */}
                                    <div className="spm-actions">
                                        <button type="button" className="spm-btn spm-btn-ghost" onClick={onClose}>
                                            Cancel
                                        </button>
                                        <ConfirmButton label="Сплатити" onSuccess={onPaid} />
                                    </div>
                                </form>
                            </Elements>
                        </div>

                        <div className="spm-footer">
                            <div className="spm-badges">
                                <span className="spm-badge">Secured Stripe</span>
                                <span className="spm-dot" />
                                <span className="spm-badge">3D Secure</span>
                                <span className="spm-dot" />
                                <span className="spm-badge">PCI DSS</span>
                            </div>
                        </div>
                    </motion.div>
                </motion.div>
            )}
        </AnimatePresence>
    );
};

export default StripePaymentModal;