import {CreateIngredientRequest} from "./CreateIngredientRequest";
import { Category } from "../../../constants/category";

export interface CreateDishRequest {
    businessId: string          // Guid
    name: string
    description?: string | null
    price: number               // decimal
    category: Category
    image?: File | null         // IFormFile?
    cookingTime: number
    ingredients: CreateIngredientRequest[]
}
