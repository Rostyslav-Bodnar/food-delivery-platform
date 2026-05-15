import {LocationResponse} from "./LocationResponse";
import {DishResponse} from "./DishResponse";

export interface OrderResponse {
    id : string
    businessId : string
    businessName : string
    orderedBy : string
    orderDate : string
    totalPrice : number
    deliveryFee : number
    courierFee : number
    courierPaid : boolean
}

export interface CustomerOrderResponse extends OrderResponse {
    businessLocation: LocationResponse;
    customerLocation: LocationResponse;
    courierLocation: LocationResponse;
    deliveredBy: string;
    courierName: string;
    orderStatus: string;
    dishes: DishResponse[];
}

export interface BusinessOrderResponse extends OrderResponse {
    businessLocation: LocationResponse;
    customerLocation: LocationResponse;
    courierLocation: LocationResponse;
    deliveredBy: string;
    courierName: string;
    orderStatus: string;
    dishes: DishResponse[];
}

export interface CourierOrderResponse extends OrderResponse {
    businessLocation: LocationResponse;
    customerLocation: LocationResponse;
    courierLocation: LocationResponse;
    orderStatus: string;
    profit: number;
}

export interface OrderDetailsResponse {
    id: string;
    businessId: string;
    businessName: string;
    orderedById: string;
    customerFullName: string;
    customerAddress: string;
    customerPhoneNumber: string;
    orderDate: string;
    totalPrice: number;
    deliveryFee: number;
    courierFee: number;
    courierPaid: boolean;
    orderStatus: string;
    profit: number;
    dishes: DishResponse[];
    deliveredById?: string | null;
    courierName?: string | null;
    courierPhoneNumber?: string | null;
}