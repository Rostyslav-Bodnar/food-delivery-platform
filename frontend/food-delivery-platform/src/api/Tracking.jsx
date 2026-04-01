import axios from "axios";

const API_BASE = "http://localhost:5185/api";

const trackingApi = axios.create({
    baseURL: API_BASE,
    withCredentials: true
});

trackingApi.interceptors.request.use((config) => {
    const token = localStorage.getItem("accessToken");
    if (token) {
        config.headers.Authorization = `Bearer ${token}`;
    }

    return config;
});

export const addLocation = async (location) => {
    const response = await trackingApi.post("/location/add", location);
    return response.data;
};

export const getBusinessLocationsByBusinessId = async (businessId) => {
    const response = await trackingApi.get(`/businesslocation/business/${businessId}`);
    return response.data;
};

export default trackingApi;
