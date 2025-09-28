import axios from 'axios';

const API_URL = 'http://localhost:5000/api/Auth';

export const register = async (userData) => {
    try {
        const response = await axios.post(`${API_URL}/register`, {
            email: userData.email,
            password: userData.password,
            fullName: userData.username
        });
        return response.data;
    } catch (error) {
        throw error.response?.data || 'Registration failed';
    }
};

export const login = async (credentials) => {
    try {
        const response = await axios.post(`${API_URL}/login`, {
            email: credentials.email,
            password: credentials.password
        });
        return response.data;
    } catch (error) {
        throw error.response?.data || 'Login failed';
    }
};