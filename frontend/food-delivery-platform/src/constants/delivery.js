// Mirrors backend/OrderService/DF.OrderService.Application/Services/ProfitService.cs
// (DeliveryFeeCalculator). Keep both sides in sync — backend is authoritative.
export const BASE_SHARE_PERCENT = 0.25;
export const RATE_PER_KM = 1.50;

export const calculateDeliveryFee = (subtotal, distanceKm) => {
    if (subtotal == null || distanceKm == null || !Number.isFinite(distanceKm)) {
        return null;
    }
    const fee = subtotal * BASE_SHARE_PERCENT + RATE_PER_KM * distanceKm;
    return Math.round(fee * 100) / 100;
};
