const BASE = "http://localhost:5003/api";

export async function getClientSecret(orderId) {
    const res = await fetch(`${BASE}/payments/${orderId}`, { credentials: 'include' });
    if (!res.ok) throw new Error("Client secret not ready");
    return res.json(); // { clientSecret: "..." }
}
