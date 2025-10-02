import axios from "axios";

const API_URL = "http://localhost:5000/api/Account";

export const getAccount = async (userId) => {
    try {
        const response = await axios.get(`${API_URL}/${userId}`);
        return response.data;
    } catch (error) {
        throw error.response?.data || "Failed to fetch account";
    }
};

export const getAccounts = async (userId) => {
    try {
        const response = await axios.get(`${API_URL}/all/${userId}`);
        return response.data;
    } catch (error) {
        throw error.response?.data || "Failed to fetch accounts";
    }
};

export const createAccount = async (accountData) => {
    try {
        const response = await axios.post(API_URL, accountData);
        return response.data;
    } catch (error) {
        throw error.response?.data || "Failed to create account";
    }
};

export const updateAccount = async (id, accountData) => {
    try {
        const response = await axios.put(`${API_URL}/${id}`, accountData);
        return response.data;
    } catch (error) {
        throw error.response?.data || "Failed to update account";
    }
};

export const deleteAccount = async (id) => {
    try {
        await axios.delete(`${API_URL}/${id}`);
        return true;
    } catch (error) {
        throw error.response?.data || "Failed to delete account";
    }
};
