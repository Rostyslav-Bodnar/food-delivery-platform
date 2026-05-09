import axios from "axios";
import { TRACKING_API_BASE } from "../config/api.js";

const trackingApi = axios.create({
    baseURL: TRACKING_API_BASE,
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
