import axios from "axios";

const API_BASE = import.meta.env.VITE_API_URL || "http://localhost:5110/api";

const dishApi = axios.create({
    baseURL: API_BASE,
    withCredentials: true
});

dishApi.interceptors.request.use((config) => {
    const token = localStorage.getItem("accessToken");
    if (token) {
        config.headers.Authorization = `Bearer ${token}`;
    }
    return config;
});

// Отримати всі страви для customer
export const getAllDishesForCustomer = async () => {
    const res = await dishApi.get(`/dish/customer`);
    return res.data;
};

// Отримати страву для customer по Id
export const getDishForCustomer = async (id) => {
    const res = await dishApi.get(`/dish/customer/${id}`);
    return res.data;
};

// Отримати страви для customer по BusinessId
export const getDishesForCustomerByBusinessId = async (businessId) => {
    const res = await dishApi.get(`/dish/customer/${businessId}/dish`);
    return res.data;
};

// Отримати всі страви (admin/business)
export const getAllDishes = async () => {
    const response = await dishApi.get(`/dish`);
    return response.data;
};

// Отримати страву по Id
export const getDishById = async (id) => {
    const response = await dishApi.get(`/dish/${id}`);
    return response.data;
};

// Отримати страви по BusinessId
export const getDishesByBusinessId = async (businessId) => {
    const response = await dishApi.get(`/dish/${businessId}/dish`);
    return response.data;
};

// Видалити страву
export const deleteDish = async (id) => {
    const response = await dishApi.delete(`/dish/${id}`);
    return response.data;
};

// Мапа категорій → enum
const categoryMap = {
    pizza: 1,
    drinks: 2,
    salad: 3,
    dessert: 4,
};

// Створити страву
export const createDish = async (dish) => {
    const formData = new FormData();

    formData.append("UserId", dish.userId);

    if (dish.menuId) formData.append("MenuId", dish.menuId);

    formData.append("Name", dish.name);
    if (dish.description) formData.append("Description", dish.description);
    formData.append("Price", dish.price);
    formData.append("CookingTime", dish.cookingTime);

    if (dish.category && categoryMap[dish.category]) {
        formData.append("Category", categoryMap[dish.category]);
    }

    if (dish.image) {
        formData.append("Image", dish.image);
    }

    if (dish.ingredients?.length > 0) {
        dish.ingredients.forEach((i, idx) => {
            formData.append(`Ingredients[${idx}].Name`, i.name);
            formData.append(`Ingredients[${idx}].Weight`, i.weight);
        });
    }

    const res = await dishApi.post(`/dish/create`, formData, {
        headers: { "Content-Type": "multipart/form-data" }
    });

    return res.data;
};

// Оновити страву
export const updateDish = async (id, body) => {
    const formData = new FormData();

    formData.append("DishId", id);
    formData.append("Name", body.name);
    formData.append("Description", body.description ?? "");
    formData.append("Price", body.price);
    formData.append("CookingTime", body.cookingTime);
    formData.append("Category", body.category);

    if (body.menuId) formData.append("MenuId", body.menuId);
    if (body.image) formData.append("Image", body.image);

    if (body.ingredients?.length > 0) {
        body.ingredients.forEach((ing, index) => {
            if (ing.id) formData.append(`Ingredients[${index}].Id`, ing.id);
            formData.append(`Ingredients[${index}].Name`, ing.name);
            formData.append(`Ingredients[${index}].Weight`, ing.weight);
        });
    }

    const response = await dishApi.post(`/dish/update`, formData, {
        headers: { "Content-Type": "multipart/form-data" }
    });

    return response.data;
};


