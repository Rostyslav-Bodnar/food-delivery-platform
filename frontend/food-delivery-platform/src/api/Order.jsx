import axios from "axios";

const API_BASE = "http://localhost:5229/api";

const orderApi = axios.create({
    baseURL: API_BASE,
    withCredentials: true
});

orderApi.interceptors.request.use((config) => {
    const token = localStorage.getItem("accessToken");
    if (token) {
        config.headers.Authorization = `Bearer ${token}`;
    }
    return config;
});

// Отримати всі замовлення
export const getAllOrders = async () => {
    const res = await orderApi.get(`/order/all`);
    return res.data;
};

// Отримати замовлення по бізнесу
export const getOrdersByBusiness = async (businessId) => {
    const res = await orderApi.get(`/order/business`, { params: { businessId } });
    return res.data;
};

// Отримати замовлення по курʼєру
export const getOrdersByCourier = async (courierId) => {
    const res = await orderApi.get(`/order/courier`, { params: { courierId } });
    return res.data;
};

// Змінити статус замовлення
export const changeOrderStatus = async (orderId, status) => {
    const res = await orderApi.patch(`/order/status`, null, { params: { orderId, status } });
    return res.data;
};

// Отримати деталі замовлення
export const getOrderDetails = async (orderId) => {
    const res = await orderApi.get(`/order/get-order-details/${orderId}`);
    return res.data;
};

// Отримати всі замовлення клієнта
export const getCustomerOrders = async (customerId) => {
    const res = await orderApi.get(`/order/get-customer-orders`, { params: { customerId } });
    return res.data;
};

// Отримати історію замовлень клієнта
export const getCustomerOrderHistory = async (customerId) => {
    const res = await orderApi.get(`/order/get-customer-history`, { params: { customerId } });
    return res.data;
};

// Отримати історію замовлень курʼєра
export const getCourierOrderHistory = async (customerId) => {
    const res = await orderApi.get(`/order/get-courier-history`, { params: { customerId } });
    return res.data;
};

// Створити замовлення
export const createOrder = async (order) => {
    const res = await orderApi.post(`/order/create-order`, order);
    return res.data;
};

// Створити кілька замовлень
export const createOrders = async (orders) => {
    const res = await orderApi.post(`/order/create-orders`, orders);
    return res.data;
};

// Курʼєр приймає замовлення на доставку
export const deliverOrder = async (orderId, courierId) => {
    const res = await orderApi.post(`/order/courier/deliver`, null, { params: { orderId, courierId } });
    return res.data;
};
