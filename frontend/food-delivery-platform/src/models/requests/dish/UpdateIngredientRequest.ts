export interface UpdateIngredientRequest {
    id?: string | null   // Guid?
    name: string
    weight: number
}