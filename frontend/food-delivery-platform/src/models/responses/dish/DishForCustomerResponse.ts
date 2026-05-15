import {Category} from "../../../constants/category";
import {IngredientResponse} from "./IngredientResponse";
import {BusinessResponse} from "./BusinessResponse";

export interface DishResponse {
    id: string
    name: string
    description?: string | null
    imageUrl?: string | null
    price: number
    category: Category
    cookingTime: number
    businessDetails: BusinessResponse
    ingredients: IngredientResponse[]
}