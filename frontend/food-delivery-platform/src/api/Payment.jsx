import { api } from "./apiClient";

// PaymentService is now proxied via the Gateway (same baseURL as Order).
// Backend wraps responses as { success, data, errorMassage }.
export async function getPaymentByOrderId(orderId) {
    const res = await api.get(`/payments/${orderId}`);
    return res.data.data;
}

export async function getClientSecret(orderId) {
    const payment = await getPaymentByOrderId(orderId);
    if (!payment?.clientSecret) {
        throw new Error("Client secret not ready");
    }
    return payment.clientSecret;
}
