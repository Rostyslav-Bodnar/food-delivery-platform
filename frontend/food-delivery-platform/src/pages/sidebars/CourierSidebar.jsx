import { Package, Bike, History, Power } from "lucide-react";
import Sidebar from "./components/Sidebar";

export default function CourierSidebar({
                                           activeOrder,
                                           history,
                                           isOnline,
                                           setIsOnline,
                                           userData
                                       }) {

    const items = [
        { id: "new", label: "New Orders", icon: Package, path: "/courier/new" },
        { id: "active", label: "Active order", icon: Bike, path: "/courier/active", badge: activeOrder ? 1 : null },
        { id: "history", label: "Order's History", icon: History, path: "/courier/history", badge: history.length },
    ];

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
                    {userData?.name?.[0] || "К"}
                </div>
                <div>
                    <div>{userData?.name}</div>
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