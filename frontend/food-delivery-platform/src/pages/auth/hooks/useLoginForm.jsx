import { useState } from "react"
import { login } from "../../../api/Auth"

const useLoginForm = () => {
    const [formData, setFormData] = useState({
        email: "",
        password: ""
    })

    const [formError, setFormError] = useState(null)
    const [systemError, setSystemError] = useState(null)
    const [submitting, setSubmitting] = useState(false)

    const handleChange = (e) => {
        setFormError(null)
        setSystemError(null)

        setFormData(prev => ({
            ...prev,
            [e.target.name]: e.target.value
        }))
    }

    const handleSubmit = async (e) => {
        e.preventDefault()

        // клієнтська валідація
        if (!formData.email || !formData.password) {
            setFormError("Please enter email and password")
            return
        }

        try {
            setSubmitting(true)

            await login(formData)
            window.location.href = "/food-delivery-platform/profile"
        } catch (err) {
            const message = err.message.toLowerCase()

            if (
                message.includes("invalid") ||
                message.includes("password") ||
                message.includes("credentials") ||
                message.includes("email")
            ) {
                setFormError(err.message)
            } else {
                setSystemError(err.message)
            }
        } finally {
            setSubmitting(false)
        }
    }

    return {
        formData,
        formError,
        systemError,
        submitting,
        handleChange,
        handleSubmit,
        clearSystemError: () => setSystemError(null)
    }
}

export default useLoginForm