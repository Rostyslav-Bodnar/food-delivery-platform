import {CreateLocationRequest} from "./CreateLocationRequest";
import {CreateOrderDishRequest} from "./CreateOrderDishRequest";

export interface CreateOrderRequest {
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