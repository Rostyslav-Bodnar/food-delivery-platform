import axios from "axios";

const API_BASE = import.meta.env.VITE_API_URL || "http://localhost:5110/api";

// Отримати всі страви для customer
export const getAllDishesForCustomer = async () => {
    const res = await axios.get(`${API_BASE}/dish/customer`);
    return res.data;
};

// Отримати страву для customer по Id
export const getDishForCustomer = async (id) => {
    const res = await axios.get(`${API_BASE}/dish/customer/${id}`);
    return res.data;
};

// Отримати страви для customer по BusinessId
export const getDishesForCustomerByBusinessId = async (businessId) => {
    const res = await axios.get(`${API_BASE}/dish/customer/${businessId}/dish`);
    return res.data;
};

// Отримати всі страви
export const getAllDishes = async (token) => {
    const response = await axios.get(`${API_BASE}/dish`, {
        headers: { Authorization: `Bearer ${token}` }
    });
    return response.data;
};

// Отримати страву по Id
export const getDishById = async (id, token) => {
    const response = await axios.get(`${API_BASE}/dish/${id}`, {
        headers: { Authorization: `Bearer ${token}` }
    });
    return response.data;
};

// Отримати страви по BusinessId
export const getDishesByBusinessId = async (businessId, token) => {
    const response = await axios.get(`${API_BASE}/dish/${businessId}/dish`, {
        headers: { Authorization: `Bearer ${token}` }
    });
    return response.data;
};

// Видалити страву
export const deleteDish = async (id, token) => {
    const response = await axios.delete(`${API_BASE}/dish/${id}`, {
        headers: { Authorization: `Bearer ${token}` }
    });
    return response.data;
};

// Мапа категорій у enum-значення бекенду
const categoryMap = {
    pizza: 1,
    drinks: 2,
    salad: 3,
    dessert: 4,
};

export const createDish = async (dish, token) => {
    const formData = new FormData();

    // UserId завжди потрібен
    formData.append("UserId", dish.userId);

    // MenuId додаємо тільки якщо воно є
    if (dish.menuId) {
        formData.append("MenuId", dish.menuId);
    }

    // Основні поля
    formData.append("Name", dish.name);
    if (dish.description) formData.append("Description", dish.description);
    formData.append("Price", dish.price);
    formData.append("CookingTime", dish.cookingTime);

    // Категорія → число
    if (dish.category && categoryMap[dish.category]) {
        formData.append("Category", categoryMap[dish.category]);
    }

    // Зображення
    if (dish.image) {
        formData.append("Image", dish.image);
    }

    // Інгредієнти
    if (dish.ingredients && dish.ingredients.length > 0) {
        dish.ingredients.forEach((i, idx) => {
            formData.append(`Ingredients[${idx}].Name`, i.name);
            formData.append(`Ingredients[${idx}].Weight`, i.weight);
        });
    }

    const res = await axios.post(`${API_BASE}/dish/create`, formData, {
        headers: {
            Authorization: `Bearer ${token}`,
            "Content-Type": "multipart/form-data",
        },
    });

    return res.data;
};


// Оновити страву (multipart/form-data)
export const updateDish = async (id, body, token) => {
    const formData = new FormData();

    formData.append("DishId", id);

    formData.append("Name", body.name);
    formData.append("Description", body.description ?? "");
    formData.append("Price", body.price);
    formData.append("CookingTime", body.cookingTime);
    formData.append("Category", body.category);

    if (body.menuId) formData.append("MenuId", body.menuId);
    if (body.image) formData.append("Image", body.image);

    if (body.ingredients && body.ingredients.length > 0) {
        body.ingredients.forEach((ing, index) => {
            if (ing.id) formData.append(`Ingredients[${index}].Id`, ing.id);
            formData.append(`Ingredients[${index}].Name`, ing.name);
            formData.append(`Ingredients[${index}].Weight`, ing.weight);
        });
    }

    const response = await axios.post(`${API_BASE}/dish/update`, formData, {
        headers: {
            Authorization: `Bearer ${token}`,
            "Content-Type": "multipart/form-data",
        },
    });

    return response.data;
};


