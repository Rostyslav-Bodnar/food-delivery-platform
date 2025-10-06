import axios from "axios";

const API_BASE = import.meta.env.VITE_API_URL || "https://localhost:5001/api";

export const getProfileData = async (token) => {
    const response = await axios.get(`${API_BASE}/profile`, {
        headers: { Authorization: `Bearer ${token}` }
    });
    return response.data;
};

export const switchAccount = async (accountId, token) => {
    const response = await axios.put(`${API_BASE}/profile/switch/${accountId}`, {}, {
        headers: { Authorization: `Bearer ${token}` }
    });
    return response.data;
};

export const updateProfile = async (userId, body, token) => {
    const response = await axios.put(`${API_BASE}/account`, body, {
        headers: { Authorization: `Bearer ${token}` },
    });
    return response.data;
};
