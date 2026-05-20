import { Home, ShoppingCart, Store, User, Package } from "lucide-react";
import Sidebar from "./components/Sidebar";

export default function CustomerSidebar() {

    const items = [
        { id: "home", label: "Home", icon: Home, path: "/" },
        { id: "cart", label: "Cart", icon: ShoppingCart, path: "/cart" },
        { id: "restaurants", label: "Restaurants", icon: Store, path: "/restaurants" },
        { id: "orders", label: "Order", icon: Package, path: "/customer/orders" },
        { id: "profile", label: "Profile", icon: User, path: "/profile" },
    ];

    return (
        <Sidebar
            items={items}
        />
    );
}