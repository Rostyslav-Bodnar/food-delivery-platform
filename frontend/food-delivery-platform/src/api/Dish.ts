import { api } from "./apiClient"
import type { ApiResponse } from "../models/responses/Response"

import type { DishResponse } from "../models/responses/dish/DishResponse"
import type { CreateDishRequest } from "../models/requests/dish/CreateDishRequest"
import type { UpdateDishRequest } from "../models/requests/dish/UpdateDishRequest"

//
// =========================
// GET — customer
// =========================
//
export async function getAllDishesForCustomer(): Promise<DishResponse[]> {
    const res = await api.get<ApiResponse<DishResponse[]>>(
        `/dish/customer`
    )

    return res.data.data!
}

export async function getDishesByBusinessId(
    businessId: string
): Promise<DishResponse[]> {
    const res = await api.get<
        ApiResponse<DishResponse[]>
    >(
        `/dish/${businessId}/dish`
    );

    return res.data.data!;
}

export async function getDishForCustomer(
    id: string
): Promise<DishResponse> {
    const res = await api.get<ApiResponse<DishResponse>>(
        `/dish/customer/${id}`
    )

    return res.data.data!
}

export async function getDishesForCustomerByBusinessId(
    businessId: string
): Promise<DishResponse[]> {
    const res = await api.get<ApiResponse<DishResponse[]>>(
        `/dish/customer/${businessId}/dish`
    )

    return res.data.data!
}

//
// =========================
// GET — admin / business
// =========================
//
export async function getAllDishes(): Promise<DishResponse[]> {
    const res = await api.get<ApiResponse<DishResponse[]>>(
        `/dish`
    )

    return res.data.data!
}

export async function getDishById(
    id: string
): Promise<DishResponse> {
    const res = await api.get<ApiResponse<DishResponse>>(
        `/dish/${id}`
    )

    return res.data.data!
}

//
// =========================
// DELETE
// =========================
//
export async function deleteDish(
    id: string
): Promise<void> {
    await api.delete(`/dish/${id}`)
}

//
// =========================
// CREATE (multipart, DTO‑driven)
// =========================
//
export async function createDish(
    request: CreateDishRequest
): Promise<DishResponse> {
    const formData = new FormData()

    formData.append("BusinessId", request.businessId)
    formData.append("Name", request.name)
    formData.append("Price", request.price.toString())
    formData.append("CookingTime", request.cookingTime.toString())
    formData.append("Category", request.category.toString())

    if (request.description)
        formData.append("Description", request.description)

    if (request.image)
        formData.append("Image", request.image)

    request.ingredients.forEach((ingredient, index) => {
        formData.append(
            `Ingredients[${index}].Name`,
            ingredient.name
        )
        formData.append(
            `Ingredients[${index}].Weight`,
            ingredient.weight.toString()
        )
    })

    const res = await api.post<ApiResponse<DishResponse>>(
        `/dish/create`,
        formData
    )

    return res.data.data!
}

//
// =========================
// UPDATE (multipart, DTO‑driven)
// =========================
//
export async function updateDish(
    request: UpdateDishRequest
): Promise<DishResponse> {
    const formData = new FormData()

    formData.append("DishId", request.dishId)
    formData.append("Name", request.name)
    formData.append("Price", request.price.toString())
    formData.append("CookingTime", request.cookingTime.toString())
    formData.append("Category", request.category.toString())

    if (request.description)
        formData.append("Description", request.description)

    if (request.image)
        formData.append("Image", request.image)

    request.ingredients.forEach((ingredient, index) => {
        if (ingredient.id) {
            formData.append(
                `Ingredients[${index}].Id`,
                ingredient.id
            )
        }

        formData.append(
            `Ingredients[${index}].Name`,
            ingredient.name
        )
        formData.append(
            `Ingredients[${index}].Weight`,
            ingredient.weight.toString()
        )
    })

    const res = await api.post<ApiResponse<DishResponse>>(
        `/dish/update`,
        formData,
        {
            headers: {
                "Content-Type": "multipart/form-data"
            }
        }
    )

    return res.data.data!
}