export interface ApiResponse<T> {
    success: boolean
    data: T | null
    errorMassage?: string
}