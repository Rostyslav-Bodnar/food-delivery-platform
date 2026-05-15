// =========================
// Base
// =========================
export interface AccountResponseBase {
    id?: string
    userId?: string
    accountType: string
    imageUrl?: string

    /** Polymorphic discriminator from backend */
    $type: "customer" | "business" | "courier"
}
export interface CustomerAccountResponse extends AccountResponseBase {
    $type: "customer"

    phoneNumber?: string
    name?: string
    surname?: string
    address?: string
}
export interface BusinessAccountResponse extends AccountResponseBase {
    $type: "business"

    name: string
    description?: string
    stripeAccountId?: string
    stripeChargesEnabled?: boolean
    stripePayoutsEnabled?: boolean
    stripeRequirementsDue?: string
    stripeOnboardedAt?: string // ⬅ ISO string (DateTime → string)
}
export interface CourierAccountResponse extends AccountResponseBase {
    $type: "courier"

    phoneNumber?: string
    name?: string
    surname?: string
    address?: string
    description?: string
}

export type AccountResponse =
    | CustomerAccountResponse
    | BusinessAccountResponse
    | CourierAccountResponse
``