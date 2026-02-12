// src/components/business/BusinessSidebar.jsx
import React from "react";
import { useNavigate, useLocation } from "react-router-dom";
import { BarChart2, Package, Filter, Users } from "lucide-react";

const SIDEBAR_ITEMS = [
    { id: "dashboard", label: "Dashboard", icon: BarChart2, path: "/" },
    { id: "orders", label: "Orders", icon: Package, path: "/business/orders" },
    { id: "dishes", label: "Menu", icon: Filter, path: "/business/dishes" },
    { id: "staff", label: "Staff", icon: Users, path: "/business/staff" },
];

export default function BusinessSidebar({ userData }) {
    const navigate = useNavigate();
    const location = useLocation();

    const businessName = userData?.currentAccount?.name || "My Restaurant";
    const businessLogo = userData?.currentAccount?.imageUrl;

    const activeId =
        SIDEBAR_ITEMS.find((item) => location.pathname === item.path)?.id ||
        "dashboard";

    return (
        <aside className="bh-sidebar">
            {/* Critical fix: load font globally + force Cyrillic support */}
            <link
                href="https://fonts.googleapis.com/css2?family=Inter:wght@400;500;600;700&subset=cyrillic,cyrillic-ext&display=swap"
                rel="stylesheet"
            />
            <style jsx global>{`
        .bh-sidebar,
        .bh-sidebar * {
          font-family: 'Inter', system-ui, -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif !important;
        }
      `}</style>

            <div className="bh-brand">
                <div className="bh-logo">
                    {businessLogo ? (
                        <img src={businessLogo} alt="Logo" />
                    ) : (
                        <div style={{ fontWeight: 800, color: "#041021" }}>F</div>
                    )}
                </div>
                <div className="bh-title">{businessName}</div>
            </div>

            <nav className="bh-nav">
                {SIDEBAR_ITEMS.map((item) => {
                    const Icon = item.icon;
                    const isActive = activeId === item.id;

                    return (
                        <button
                            key={item.id}
                            className={`bh-nav-item ${isActive ? "active" : ""}`}
                            onClick={() => navigate(item.path)}
                        >
                            <Icon size={18} />
                            <span>{item.label}</span>
                        </button>
                    );
                })}
            </nav>
        </aside>
    );
}