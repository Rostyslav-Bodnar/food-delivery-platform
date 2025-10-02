import axios from "axios";

const API_URL = "http://localhost:5000/api/User";

// Отримати свій профіль (потрібен токен!)
export const getProfile = async (token) => {
    try {
        const response = await axios.get(`${API_URL}/profile`, {
            headers: {
                Authorization: `Bearer ${token}`,
            },
        });
        return response.data;
    } catch (error) {
        throw error.response?.data || "Failed to fetch profile";
    }
};

// Отримати конкретного користувача
export const getUser = async (userId) => {
    try {
        const response = await axios.get(`${API_URL}/user`, {
            params: { userId },
        });
        return response.data;
    } catch (error) {
        throw error.response?.data || "Failed to fetch user";
    }
};

// Отримати всіх користувачів
export const getUsers = async () => {
    try {
        const response = await axios.get(`${API_URL}/users`);
        return response.data;
    } catch (error) {
        throw error.response?.data || "Failed to fetch users";
    }
};
