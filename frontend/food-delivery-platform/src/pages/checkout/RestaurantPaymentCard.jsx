import React, { useMemo, useState } from 'react';
import { loadStripe } from '@stripe/stripe-js';
import { Elements, PaymentElement, useStripe, useElements } from '@stripe/react-stripe-js';

const stripePromise = loadStripe('pk_test_51SeMlmL2Z4y20S3ELazXa1alP4LwhOeL91Jd54mZcEE4f0e5e7pKRnpMLdFh2uNNVbUhEyC8JF7jjKfyAneWhAPT00bgasYuIv');

const ConfirmButton = ({ onSuccess }) => {
    const stripe = useStripe();
    const elements = useElements();
    const [loading, setLoading] = useState(false);

    const handlePay = async (e) => {
        e.preventDefault();
        if (!stripe || !elements) return;

        setLoading(true);
        const { error } = await stripe.confirmPayment({
            elements,
            redirect: 'if_required' // 3DS відкриється у модалці
            // confirmParams: { return_url: 'https://...' } // якщо хочеш явно
        });
        setLoading(false);

        if (error) {
            alert(error.message || "Payment declined");
        } else {
            // Фінальний статус все одно приходить у webhook; тут просто UX-успіх
            onSuccess?.();
        }
    };

    return (
        <button className="btn btn-primary" onClick={handlePay} disabled={!stripe || loading}>
            {loading ? "Processing..." : "Pay"}
        </button>
    );
};

const RestaurantPaymentCard = ({ clientSecret, onSuccess }) => {
    const options = useMemo(() => ({ clientSecret, appearance: { theme: 'stripe' } }), [clientSecret]);
    if (!clientSecret) return null;

    return (
        <Elements stripe={stripePromise} options={options}>
            <form onSubmit={(e) => e.preventDefault()}>
                <PaymentElement />
                <div style={{ marginTop: 12 }}>
                    <ConfirmButton onSuccess={onSuccess} />
                </div>
            </form>
        </Elements>
    );
};

export default RestaurantPaymentCard;