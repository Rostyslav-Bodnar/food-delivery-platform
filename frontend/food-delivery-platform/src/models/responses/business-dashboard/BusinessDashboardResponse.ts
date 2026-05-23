export interface BusinessDashboardSummary {
    grossSales: number
    refunds: number
    platformFees: number
    stripeProcessingFees: number
    netIncome: number
    paidOut: number
    orders: number
    refundedOrders: number
}

export interface DashboardTimePoint {
    date: string         // ISO date
    gross: number
    refunds: number
    fees: number
    net: number
}

export interface DashboardBreakdownItem {
    category: string
    amount: number
}

export interface DashboardPayout {
    id: string
    createdAtUtc: string
    arrivalUtc?: string | null
    paidAtUtc?: string | null
    amount: number
    currency: string
    status: string
    failureMessage?: string | null
}

export interface BusinessBalanceSnapshot {
    available: number
    pending: number
    currency: string
}

export interface DashboardWindow {
    fromUtc: string
    toUtc: string
}

export interface BusinessDashboardResponse {
    summary: BusinessDashboardSummary
    timeSeries: DashboardTimePoint[]
    outcomeBreakdown: DashboardBreakdownItem[]
    payouts: DashboardPayout[]
    balance: BusinessBalanceSnapshot
    window: DashboardWindow
    currency: string
}

export interface DishRevenue {
    dishId: string
    dishName: string
    quantitySold: number
    orderCount: number
    revenue: number
}
