import axios from "axios";

const API_URL = "http://localhost:5000/api/Auth";

const authApi = axios.create({
    baseURL: API_URL,
    withCredentials: true // ✅ дозволяє відправляти HttpOnly refresh cookie
});

authApi.interceptors.request.use(config => {
    const token = localStorage.getItem("accessToken");
    if (token) {
        config.headers["Authorization"] = `Bearer ${token}`;
    }
    return config;
});

// 📦 API
export const register = async (userData) => {
    const response = await authApi.post("/register", {
        email: userData.email,
        password: userData.password,
        name: userData.name,
        surname: userData.surname
    });
    saveTokens(response.data);
    return response.data;
};

export const login = async (credentials) => {
    const response = await authApi.post("/login", {
        email: credentials.email,
        password: credentials.password
    });
    saveTokens(response.data);
    return response.data;
};

export const refresh = async () => {
    const response = await authApi.post("/refresh", {}, { withCredentials: true });
    saveTokens(response.data);
    return response.data;
};

export const logout = async () => {
    await authApi.post("/revoke", {}, { withCredentials: true });
    clearTokens();
};

function saveTokens(tokens) {
    localStorage.setItem("accessToken", tokens.accessToken);
    localStorage.setItem("accessTokenExpiresAt", tokens.accessTokenExpiresAt);
}

function clearTokens() {
    localStorage.removeItem("accessToken");
    localStorage.removeItem("accessTokenExpiresAt");
}

export default authApi;
