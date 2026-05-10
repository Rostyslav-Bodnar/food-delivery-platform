import {UpdateIngredientRequest} from "./UpdateIngredientRequest";
import { Category } from "../../../constants/category";

export interface UpdateDishRequest {
    dishId: string              // Guid
    name: string
    description?: string | null
    price: number               // decimal
    category: Category
    cookingTime: number
    image?: File | null         // IFormFile?
    ingredients: UpdateIngredientRequest[]
}
