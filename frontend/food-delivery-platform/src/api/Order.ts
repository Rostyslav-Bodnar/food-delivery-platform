import { api } from "./apiClient"

import type { ApiResponse } from "../models/responses/Response"
import type {
    OrderResponse,
    OrderDetailsResponse,
    BusinessOrderResponse,
    CustomerOrderResponse,
    CourierOrderResponse
} from "../models/responses/order/OrderResponse"

import type { CreateOrderRequest } from "../models/requests/order/CreateOrderRequest"
import type { OrderStatus } from "../models/enums/OrderStatus"

// =========================
// GET ALL ORDERS
// =========================
export async function getAllOrders(): Promise<OrderResponse[]> {
    const res = await api.get<ApiResponse<OrderResponse[]>>(
        "/order/all"
    )

    return res.data.data!
}

// =========================
// GET ORDER DETAILS
// =========================
export async function getOrderDetails(
    orderId: string
): Promise<OrderDetailsResponse> {
    const res = await api.get<ApiResponse<OrderDetailsResponse>>(
        `/order/details/${orderId}`
    )

    return res.data.data!
}

// =========================
// GET BY BUSINESS
// =========================
export async function getOrdersByBusiness(
    businessId: string
): Promise<BusinessOrderResponse[]> {
    const res = await api.get<ApiResponse<BusinessOrderResponse[]>>(
        "/order/business",
            { params: { businessId } }
    )

    return res.data.data!
}

// =========================
// GET BY COURIER
// =========================
export async function getOrdersByCourier(
    courierId: string
): Promise<CourierOrderResponse[]> {
    const res = await api.get<ApiResponse<CourierOrderResponse[]>>(
        "/order/courier",
            { params: { courierId } }
    )

    return res.data.data!
}

// =========================
// ACTIVE COURIER ORDERS
// =========================
export async function getActiveCourierOrders(
    courierId: string
): Promise<CourierOrderResponse[]> {
    const res = await api.get<ApiResponse<CourierOrderResponse[]>>(
        "/order/courier/active",
            { params: { courierId } }
    )

    return res.data.data!
}

// =========================
// CHANGE ORDER STATUS
// =========================
export async function changeOrderStatus(
    orderId: string,
    status: OrderStatus
): Promise<OrderResponse> {
    const res = await api.patch<ApiResponse<OrderResponse>>(
        "/order/status",
            null,
            { params: { orderId, status } }
    )

    return res.data.data!
}

// =========================
// CANCEL ORDER
// =========================
export async function cancelOrder(
    orderId: string
): Promise<boolean> {
    const res = await api.patch<ApiResponse<boolean>>(
        "/order/cancel",
            null,
            { params: { orderId } }
    )

    return res.data.data!
}

// =========================
// GET CUSTOMER ORDERS
// =========================
export async function getCustomerOrders(
    customerId: string
): Promise<CustomerOrderResponse[]> {
    const res = await api.get<ApiResponse<CustomerOrderResponse[]>>(
        `/order/customer/${customerId}`
    )

    return res.data.data!
}

// =========================
// GET CUSTOMER HISTORY
// =========================
export async function getCustomerOrderHistory(
    customerId: string
): Promise<CustomerOrderResponse[]> {
    const res = await api.get<ApiResponse<CustomerOrderResponse[]>>(
        `/order/customer/${customerId}/history`
    )

    return res.data.data!
}

// =========================
// GET COURIER HISTORY
// =========================
export async function getCourierOrderHistory(
    courierId: string
): Promise<CourierOrderResponse[]> {
    const res = await api.get<ApiResponse<CourierOrderResponse[]>>(
        `/order/courier/${courierId}/history`
    )

    return res.data.data!
}

// =========================
// CREATE ORDER
// =========================
export async function createOrder(
    request: CreateOrderRequest
): Promise<boolean> {
    const res = await api.post<ApiResponse<boolean>>(
        "/order/create",
            request
    )

    return res.data.data!
}

// =========================
// CREATE ORDERS (BATCH)
// =========================
export async function createOrders(
    request: CreateOrderRequest[]
): Promise<boolean> {
    const res = await api.post<ApiResponse<boolean>>(
        "/order/create/batch",
            request
    )

    return res.data.data!
}

// =========================
// DELIVER ORDER
// =========================
export async function deliverOrder(
    orderId: string,
    courierId: string
): Promise<OrderResponse> {
    const res = await api.post<ApiResponse<OrderResponse>>(
        "/order/courier/deliver",
            null,
            { params: { orderId, courierId } }
    )

    return res.data.data!
}