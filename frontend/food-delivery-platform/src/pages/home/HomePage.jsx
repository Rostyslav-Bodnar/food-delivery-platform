import React, { useEffect, useState } from "react"
import "./styles/HomePage.css"

import { getCurrentUser } from "../../api/User"

import BusinessHomePage from "./business/BusinessHomePage"
import CourierHomePage from "./courier/CourierHomePage"
import CustomerHomePage from "./customer/CustomerHomePage"
import UnauthenticatedHome from "./components/UnauthenticatedHome"

const HomePage = () => {
    const [userData, setUserData] = useState(null)
    const [accountType, setAccountType] = useState(null)
    const [loading, setLoading] = useState(true)
    const [error, setError] = useState(null)

    const loadUser = async () => {
        try {
            setLoading(true)
            setError(null)

            const token = localStorage.getItem("accessToken")

            // ✅ guest — не помилка
            if (!token) {
                setUserData(null)
                return
            }

            const user = await getCurrentUser()

            setUserData(user)
            setAccountType(user.currentAccount?.accountType)

            if (user.currentAccount?.name) {
                localStorage.setItem(
                    "currentAccountName",
                    user.currentAccount.name
                )
            }
        } catch (err) {
            // ✅ system error
            setUserData(null);
        } finally {
            setLoading(false)
        }
    }

    useEffect(() => {
        loadUser()
    }, [])

    // =========================
    // LOADING
    // =========================
    if (loading) {
        return (
            <div className="page-wrapper center">
                <div className="loading-spinner">Loading…</div>
            </div>
        )
    }

    // =========================
    // MAIN CONTENT (без return всередині)
    // =========================
    let content

    if (!userData) {
        content = <UnauthenticatedHome />
    } else {
        switch (accountType?.toLowerCase()) {
            case "customer":
                content = <CustomerHomePage />
                break
            case "business":
                content = <BusinessHomePage userData={userData} />
                break
            case "courier":
                content = <CourierHomePage userData={userData} />
                break
            default:
                content = <UnauthenticatedHome />
        }
    }

    return content;

}

export default HomePage