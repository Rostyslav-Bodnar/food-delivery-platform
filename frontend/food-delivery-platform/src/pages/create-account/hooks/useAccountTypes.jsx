// hooks/useAccountTypes.js
import { useEffect, useState, useCallback } from "react"
import { getAccounts } from "../../../api/Account"

const ALL_ACCOUNT_TYPES = ["Customer", "Business", "Courier"]

export const useAccountTypes = (user) => {
    const [accountType, setAccountType] = useState(null)
    const [existingAccounts, setExistingAccounts] = useState([])
    const [loading, setLoading] = useState(true)
    const [error, setError] = useState(null)

    const fetchAccounts = useCallback(async () => {
        if (!user) return

        try {
            setLoading(true)
            setError(null)

            const accounts = await getAccounts(user.id)
            const existingTypes = accounts.map(a => a.accountType)

            setExistingAccounts(existingTypes)

            const availableType = ALL_ACCOUNT_TYPES.find(
                type => !existingTypes.includes(type)
            )

            setAccountType(availableType || null)
        } catch (err) {
            setError(err.message || "Failed to load accounts")
        } finally {
            setLoading(false)
        }
    }, [user])

    useEffect(() => {
        fetchAccounts()
    }, [fetchAccounts])

    const availableAccountTypes = ALL_ACCOUNT_TYPES.filter(
        type => !existingAccounts.includes(type)
    )

    return {
        accountType,
        setAccountType,
        availableAccountTypes,
        loading,
        error,
        retry: fetchAccounts
    }
}