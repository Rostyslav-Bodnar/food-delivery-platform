import { Home, Package, Power } from "lucide-react";
import Sidebar from "./components/Sidebar";

export default function CourierSidebar({
    isOnline,
    setIsOnline,
    userData,
    availableCount = 0,
    activeCount = 0
}) {
    const items = [
        { id: "home", label: "Home", icon: Home, path: "/" },
        {
            id: "orders",
            label: activeCount ? "Orders / Active" : "Orders",
            icon: Package,
            path: "/courier/orders",
            badge: activeCount || availableCount || null
        }
    ];

    const displayName = userData?.currentAccount?.name || userData?.name || "Courier";

    const footer = (
        <>
            <button
                className={`online-toggle ${isOnline ? "online" : "offline"}`}
                onClick={() => setIsOnline(!isOnline)}
            >
                <Power size={18} />
                {isOnline ? "Online" : "Offline"}
            </button>

            <div className="courier-info">
                <div className="courier-avatar">
                    {displayName[0] || "K"}
                </div>
                <div>
                    <div>{displayName}</div>
                </div>
            </div>
        </>
    );

    return (
        <Sidebar
            logo="FoodEx Courier"
            items={items}
            footer={footer}
        />
    );
}
