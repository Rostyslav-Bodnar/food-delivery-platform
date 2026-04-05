import React from "react";

import "./styles/CustomerOrdersPage.css";
import CustomerSidebar from "../sidebars/CustomerSidebar";
import OrderDetailsComponent from "../order-details-modal/OrderDetailsComponent.jsx";

import CustomerOrdersHeader from "./components/CustomerOrdersHeader";
import CustomerOrdersContent from "./components/CustomerOrdersContent";
import CancelOrderModal from "./components/CancelOrderModal.jsx";
import OrderTrackingModal from "./components/OrderTrackingModal.jsx";

import { useCustomerOrders } from "./hooks/useCustomerOrders";
import { useCustomerOrderSelection } from "./hooks/useCustomerOrderSelection";
import { useCustomerOrderStatusMeta } from "./hooks/useCustomerOrderStatusMeta";
import { geocodeAddress } from "../../utils/locationSearch.js";
import { formatLocation } from "../../utils/orderLocations.js";
import { getRoadRoute } from "../../utils/roadRouting.js";

const CustomerOrdersPage = () => {
    const customerId = localStorage.getItem("currentAccountId");

    const {
        orders,
        loading,
        error,
        cancellingOrderId,
        cancelCustomerOrder,
        getTrackingDetails
    } = useCustomerOrders(customerId);
    const {
        selectedOrder,
        openOrderDetails,
        closeOrderDetails
    } = useCustomerOrderSelection();
    const { getStatusMeta } = useCustomerOrderStatusMeta();

    const [orderToCancel, setOrderToCancel] = React.useState(null);
    const [trackingState, setTrackingState] = React.useState({
        loading: false,
        error: "",
        order: null,
        tracking: null
    });

    const handleTrackOrder = async (order) => {
        setTrackingState({
            loading: true,
            error: "",
            order,
            tracking: null
        });

        try {
            const details = await getTrackingDetails(order.id);
            const [restaurantCoords, customerCoords] = await Promise.all([
                order.businessCoords
                    ? Promise.resolve(order.businessCoords)
                    : geocodeAddress(order.address),
                order.customerCoords
                    ? Promise.resolve(order.customerCoords)
                    : details.customerAddress
                        ? geocodeAddress(details.customerAddress)
                        : Promise.resolve(null)
            ]);

            const route = restaurantCoords && customerCoords
                ? await getRoadRoute(restaurantCoords, customerCoords)
                : null;

            setTrackingState({
                loading: false,
                error: "",
                order,
                tracking: {
                    restaurantAddress: formatLocation(order.businessLocation) || order.address,
                    customerAddress: formatLocation(order.customerLocation) || details.customerAddress || "Address not available",
                    restaurantCoords,
                    customerCoords,
                    route
                }
            });
        } catch (trackError) {
            console.error(trackError);
            setTrackingState({
                loading: false,
                error: "Could not build the tracking map for this order.",
                order,
                tracking: null
            });
        }
    };

    const closeTrackingModal = () => {
        setTrackingState({
            loading: false,
            error: "",
            order: null,
            tracking: null
        });
    };

    const confirmCancelOrder = async () => {
        if (!orderToCancel) {
            return;
        }

        try {
            await cancelCustomerOrder(orderToCancel.id);
            if (selectedOrder?.id === orderToCancel.id) {
                closeOrderDetails();
            }
            if (trackingState.order?.id === orderToCancel.id) {
                closeTrackingModal();
            }
            setOrderToCancel(null);
        } catch (cancelError) {
            alert(cancelError.message);
        }
    };

    return (
        <div className="app-wrapper">
            <CustomerSidebar />

            <main className="auth-homepage customer-orders-page">
                <CustomerOrdersHeader />

                {trackingState.loading && (
                    <div className="orders-feedback-card">
                        <h3>Building tracking map</h3>
                        <p>We are locating the restaurant and delivery destination.</p>
                    </div>
                )}

                {trackingState.error && (
                    <div className="orders-feedback-card orders-feedback-card--error">
                        <h3>Tracking unavailable</h3>
                        <p>{trackingState.error}</p>
                    </div>
                )}

                <CustomerOrdersContent
                    loading={loading}
                    error={error}
                    orders={orders}
                    getStatusMeta={getStatusMeta}
                    onOpenDetails={openOrderDetails}
                    onTrackOrder={handleTrackOrder}
                    onRequestCancel={setOrderToCancel}
                    cancellingOrderId={cancellingOrderId}
                />
            </main>

            {selectedOrder && (
                <OrderDetailsComponent
                    order={selectedOrder}
                    statusMap={{
                        preparing: { label: "Preparing", icon: () => <span>рџЌі</span> },
                        "on-the-way": { label: "On the way", icon: () => <span>рџЏЌпёЏ</span> },
                        new: { label: "New", icon: () => <span>рџ“¦</span> },
                        cancelled: { label: "Cancelled", icon: () => <span>вњ•</span> },
                        delivered: { label: "Delivered", icon: () => <span>вњ…</span> }
                    }}
                    onClose={closeOrderDetails}
                />
            )}

            {orderToCancel && (
                <CancelOrderModal
                    order={orderToCancel}
                    cancelling={cancellingOrderId === orderToCancel.id}
                    onClose={() => setOrderToCancel(null)}
                    onConfirm={confirmCancelOrder}
                />
            )}

            {trackingState.order && trackingState.tracking && (
                <OrderTrackingModal
                    order={trackingState.order}
                    tracking={trackingState.tracking}
                    onClose={closeTrackingModal}
                />
            )}
        </div>
    );
};

export default CustomerOrdersPage;
