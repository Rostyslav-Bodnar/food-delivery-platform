import {CreateLocationRequest} from "./CreateLocationRequest";

export interface CreateOrderDishRequest {
    businessId : string
    orderedBy : string
    orderDate : string
    totalPrice : number
    deliveredBy? : string | null
    deliverTo : CreateLocationRequest
    deliverFrom : CreateLocationRequest
    paymentMethod : number
    dishes : CreateOrderDishRequest[]
}