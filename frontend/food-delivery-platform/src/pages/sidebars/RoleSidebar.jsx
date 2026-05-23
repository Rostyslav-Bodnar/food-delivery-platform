import React from "react";
import { useUser } from "../../context/UserContext";
import CustomerSidebar from "./CustomerSidebar";
import BusinessSidebar from "./BusinessSidebar";
import CourierSidebar from "./CourierSidebar";

/**
 * Role-aware sidebar wrapper. Reads the active account's role from UserContext
 * (with a localStorage fallback so the first render before the context is
 * hydrated still shows the right shell) and dispatches to the matching
 * Customer/Business/Courier sidebar.
 *
 * All props are forwarded — pages pass whichever role-specific props they
 * already use today (e.g. CourierOrdersPage passes isOnline + counts;
 * BusinessOrdersPage passes userData). Unused props are ignored by the
 * concrete sidebar component.
 */
export default function RoleSidebar(props) {
    const ctx = useUser?.();

    const ctxAccount = ctx?.accounts?.find?.(
        (a) => a.id === ctx?.currentAccountId
    );
    const ctxType = ctxAccount?.accountType;

    const storedType = typeof window !== "undefined"
        ? window.localStorage.getItem("currentAccountType")
        : null;

    const role = String(ctxType ?? storedType ?? "").toLowerCase();

    switch (role) {
        case "business":
            return <BusinessSidebar {...props} />;
        case "courier":
            return <CourierSidebar {...props} />;
        case "customer":
        default:
            return <CustomerSidebar {...props} />;
    }
}
