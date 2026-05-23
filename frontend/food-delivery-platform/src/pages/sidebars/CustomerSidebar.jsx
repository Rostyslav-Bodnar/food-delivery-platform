import { Home, ShoppingCart, Store, User, Package, History } from "lucide-react";
import Sidebar from "./components/Sidebar";

export default function CustomerSidebar() {

    const items = [
        { id: "home", label: "Home", icon: Home, path: "/" },
        { id: "cart", label: "Cart", icon: ShoppingCart, path: "/cart" },
        { id: "restaurants", label: "Restaurants", icon: Store, path: "/restaurants" },
        { id: "orders", label: "Orders", icon: Package, path: "/customer/orders" },
        { id: "history", label: "History", icon: History, path: "/customer/orders/history" },
        { id: "profile", label: "Profile", icon: User, path: "/profile" },
    ];

    return (
        <Sidebar
            items={items}
        />
    );
}