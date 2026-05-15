import { BarChart2, Package, Filter, Users } from "lucide-react";
import Sidebar from "./components/Sidebar.jsx";

export default function BusinessSidebar({ userData, disabled }) {

    const items = [
        { id: "dashboard", label: "Dashboard", icon: BarChart2, path: "/" },
        { id: "orders", label: "Orders", icon: Package, path: "/business/orders" },
        { id: "menu", label: "Menu", icon: Filter, path: "/business/dishes" },
        { id: "staff", label: "Staff", icon: Users, path: "/business/staff" },
    ];

    return (
        <Sidebar
            logo="FoodEx"
            title={userData?.currentAccount?.name}
            items={items}
            disabled={disabled}
        />
    );
}