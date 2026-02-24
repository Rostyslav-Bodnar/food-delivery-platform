import React from "react";
import { NavLink } from "react-router-dom";
import "./styles/Sidebar.css";

export default function Sidebar({
                                    logo,
                                    title,
                                    items = [],
                                    footer = null
                                }) {
    return (
        <aside className="app-sidebar">

            <div className="sidebar-header">
                {logo && <div className="sidebar-logo">{logo}</div>}
                {title && <div className="sidebar-title">{title}</div>}
            </div>

            <nav className="sidebar-nav">
                {items.map(item => (
                    <NavLink
                        key={item.id}
                        to={item.path}
                        className={({ isActive }) =>
                            `sidebar-item ${isActive ? "active" : ""}`
                        }
                    >
                        <item.icon size={20} />
                        <span>{item.label}</span>

                        {item.badge && (
                            <div className="sidebar-badge">
                                {item.badge}
                            </div>
                        )}
                    </NavLink>
                ))}
            </nav>

            {footer && (
                <div className="sidebar-footer">
                    {footer}
                </div>
            )}

        </aside>
    );
}