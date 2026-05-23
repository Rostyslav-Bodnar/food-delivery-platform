import { useEffect, useMemo, useState } from "react";
import {
    getActiveCourierOrders,
    getOrdersByCourier,
    getCourierOrderHistory
} from "../../../../api/Order.ts";
import { getCourierDeliveryStage, mapCourierOrder } from "../../../courier-orders/courierOrderUtils.js";

const useOrderDelivery = (userData) => {
    const [availableOrders, setAvailableOrders] = useState([]);
    const [activeOrders, setActiveOrders] = useState([]);
    const [history, setHistory] = useState([]);
    const [loading, setLoading] = useState(true);

    useEffect(() => {
        const courierId = userData?.currentAccount?.id;

        if (!courierId) {
            setLoading(false);
            return undefined;
        }

        let isMounted = true;

        const load = async () => {
            try {
                const [available, active, historyData] = await Promise.all([
                    getOrdersByCourier(courierId),
                    getActiveCourierOrders(courierId),
                    getCourierOrderHistory(courierId)
                ]);

                if (!isMounted) {
                    return;
                }

                setAvailableOrders(available.map(mapCourierOrder));
                setActiveOrders(active.map(mapCourierOrder));
                setHistory(historyData.map(mapCourierOrder));
            } catch (error) {
                console.error("Error while loading courier dashboard:", error);
            } finally {
                if (isMounted) {
                    setLoading(false);
                }
            }
        };

        load();
        const intervalId = window.setInterval(load, 30000);

        return () => {
            isMounted = false;
            window.clearInterval(intervalId);
        };
    }, [userData]);

    const activeOrder = activeOrders[0] ?? null;

    const stats = useMemo(() => {
        const totalRevenue = history.reduce((sum, order) => sum + Number(order.courierFee ?? 0), 0);

        return {
            availableCount: availableOrders.length,
            activeCount: activeOrders.length,
            deliveredCount: history.length,
            totalRevenue
        };
    }, [availableOrders, activeOrders, history]);

    return {
        loading,
        availableOrders,
        activeOrders,
        activeOrder,
        history,
        stats,
        activeStage: activeOrder
            ? getCourierDeliveryStage(activeOrder.id, activeOrder.orderStatus)
            : "pickup"
    };
};

export default useOrderDelivery;
