// hooks/useAccountSubmit.js
import { useState } from "react"
import { useNavigate } from "react-router-dom"
import { useUser } from "../../../context/UserContext"
import { createAccount } from "../../../api/Account"

export const useAccountSubmit = ({
                                     endpoint,
                                     buildAccountPayload,
                                     formData
                                 }) => {
    const navigate = useNavigate()
    const { reloadUser } = useUser()

    const [submitting, setSubmitting] = useState(false)
    const [error, setError] = useState(null)

    const handleSubmit = async (e) => {
        e.preventDefault()

        try {
            setSubmitting(true)
            setError(null)

            const payload = buildAccountPayload(formData)

            await createAccount(endpoint, payload)
            await reloadUser()

            navigate("/profile")
        } catch (err) {
            // ✅ err.message прийшов з Gateway → axios interceptor
            setError(err.message || "Failed to create account")
        } finally {
            setSubmitting(false)
        }
    }

    return {
        handleSubmit,
        submitting,
        error,
        clearError: () => setError(null)
    }
}
