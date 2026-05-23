import { useEffect, useState } from "react";
import {
    getBusinessDashboard,
    getRevenueByDish
} from "../../../api/BusinessDashboard";

/**
 * Loads the Stripe-backed dashboard summary AND the per-dish revenue
 * aggregate from OrderService in parallel. Re-fetches whenever the
 * businessId or windowDays changes.
 */
export default function useBusinessDashboard(businessId, windowDays) {
    const [dashboard, setDashboard] = useState(null);
    const [dishRevenue, setDishRevenue] = useState([]);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState(null);

    useEffect(() => {
        if (!businessId) {
            setLoading(false);
            return undefined;
        }

        let cancelled = false;

        const load = async () => {
            setLoading(true);
            setError(null);

            const to = new Date();
            const from = new Date(to.getTime() - windowDays * 24 * 60 * 60 * 1000);

            try {
                const [dash, dishes] = await Promise.all([
                    getBusinessDashboard(businessId, from, to),
                    getRevenueByDish(businessId, from, to).catch(() => [])
                    // Revenue-by-dish failing shouldn't blank the main dashboard;
                    // worst case the bar chart shows empty.
                ]);
                if (cancelled) return;
                setDashboard(dash);
                setDishRevenue(dishes);
            } catch (err) {
                if (!cancelled) setError(err);
            } finally {
                if (!cancelled) setLoading(false);
            }
        };

        load();

        return () => {
            cancelled = true;
        };
    }, [businessId, windowDays]);

    return { dashboard, dishRevenue, loading, error };
}
