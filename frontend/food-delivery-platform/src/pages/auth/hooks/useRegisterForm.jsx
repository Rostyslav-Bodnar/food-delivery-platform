// hooks/useRegisterForm.js
import { useState } from "react"
import { register } from "../../../api/Auth"

const useRegisterForm = () => {
    const [formData, setFormData] = useState({
        name: "",
        surname: "",
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

        // ✅ client-side validation
        if (!formData.name || !formData.surname || !formData.email || !formData.password) {
            setFormError("Please fill in all fields")
            return
        }

        if (formData.password.length < 6) {
            setFormError("Password must be at least 6 characters long")
            return
        }

        try {
            setSubmitting(true)

            await register(formData)
            window.location.href = "/profile"
        } catch (err) {
            const message = err.message?.toLowerCase() ?? ""

            // ✅ UX‑помилки, які користувач може виправити
            if (
                message.includes("email") ||
                message.includes("already") ||
                message.includes("exists") ||
                message.includes("password")
            ) {
                setFormError(err.message)
            } else {
                // ✅ системні помилки
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

export default useRegisterForm