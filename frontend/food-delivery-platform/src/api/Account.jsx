﻿import axios from "axios";

const API_URL = "http://localhost:5000/api/Account";

const accountApi = axios.create({
    baseURL: API_URL,
    withCredentials: true
});

accountApi.interceptors.request.use(config => {
    const token = localStorage.getItem("accessToken");
    if (token) {
        config.headers["Authorization"] = `Bearer ${token}`;
    }
    return config;
});

export const getAccount = async (userId) => {
    const response = await accountApi.get(`/${userId}`);
    return response.data;
};

export const getAccounts = async (userId) => {
    const response = await accountApi.get(`/all/${userId}`);
    return response.data;
};

export const createAccount = async (accountData) => {
    const response = await accountApi.post("/", accountData);
    return response.data;
};

export const updateAccount = async (id, accountData) => {
    const response = await accountApi.put(`/${id}`, accountData);
    return response.data;
};

export const deleteAccount = async (id) => {
    await accountApi.delete(`/${id}`);
    return true;
};

export default accountApi;
