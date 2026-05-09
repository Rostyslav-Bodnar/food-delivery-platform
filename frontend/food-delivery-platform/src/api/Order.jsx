import axios from "axios";
import { ORDER_API_BASE } from "../config/api.js";

const orderApi = axios.create({
    baseURL: ORDER_API_BASE,
    withCredentials: true
});

orderApi.interceptors.request.use((config) => {
    const token = localStorage.getItem("accessToken");
    if (token) {
        config.headers.Authorization = `Bearer ${token}`;
    }

    return config;
});

export const getAllOrders = async () => {
    const res = await orderApi.get("/order/all");
    return res.data;
};

export const getOrdersByBusiness = async (businessId) => {
    const res = await orderApi.get("/order/business", { params: { businessId } });
    return res.data;
};

export const getOrdersByCourier = async (courierId) => {
    const res = await orderApi.get("/order/courier", { params: { courierId } });
    return res.data;
};

export const getAvailableCourierOrders = async (courierId) => {
    const res = await orderApi.get("/order/courier", { params: { courierId } });
    return res.data;
};

export const getActiveCourierOrders = async (courierId) => {
    const res = await orderApi.get("/order/courier/active", { params: { courierId } });
    return res.data;
};

export const changeOrderStatus = async (orderId, status) => {
    const res = await orderApi.patch("/order/status", null, { params: { orderId, status } });
    return res.data;
};

export const cancelOrder = async (orderId) => {
    const res = await orderApi.patch("/order/cancel", null, { params: { orderId } });
    return res.data;
};

export const getOrderDetails = async (orderId) => {
    const res = await orderApi.get(`/order/get-order-details/${orderId}`);
    return res.data;
};

export const getTrackingAccessToken = async (orderId) => {
    const res = await orderApi.post(`/orders/${orderId}/tracking-token`);
    return res.data;
};

export const getCustomerOrders = async (customerId) => {
    const res = await orderApi.get("/order/get-customer-orders", { params: { customerId } });
    return res.data;
};

export const getCustomerOrderHistory = async (customerId) => {
    const res = await orderApi.get("/order/get-customer-history", { params: { customerId } });
    return res.data;
};

export const getCourierOrderHistory = async (courierId) => {
    const res = await orderApi.get("/order/get-courier-history", { params: { courierId } });
    return res.data;
};

export const createOrder = async (order) => {
    const res = await orderApi.post("/order/create-order", order);
    return res.data;
};

export const createOrders = async (orders) => {
    const res = await orderApi.post("/order/create-orders", orders);
    return res.data;
};

export const deliverOrder = async (orderId, courierId) => {
    const res = await orderApi.post("/order/courier/deliver", null, { params: { orderId, courierId } });
    return res.data;
};

export default orderApi;
