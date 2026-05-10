// src/pages/restaurants/hooks/useFetchRestaurants.js
import { useState, useEffect, useCallback } from "react"
import { getAllBusinessAccounts } from "../../../api/Account"

const useFetchRestaurants = () => {
    const [restaurants, setRestaurants] = useState([])
    const [loading, setLoading] = useState(true)
    const [error, setError] = useState(null)

    const fetchRestaurants = useCallback(async () => {
        try {
            setLoading(true)
            setError(null)

            const response = await getAllBusinessAccounts()

            setRestaurants(
                response.map(r => ({
                    id: r.id,
                    name: r.name,
                    image: r.imageUrl,
                    description: r.description,
                    // placeholders
                    rating: 4.8,
                    deliveryTime: "25-40 хв",
                    deliveryPrice: "Free",
                    category: r.description ?? "Restaurant"
                }))
            )
        } catch (err) {
            setError(err.message || "Failed to load restaurants")
        } finally {
            setLoading(false)
        }
    }, [])

    const clearError = () => setError(null);

    useEffect(() => {
        fetchRestaurants()
    }, [fetchRestaurants])

    return {
        restaurants,
        loading,
        error,
        retry: fetchRestaurants,
        clearError
    }
}

export default useFetchRestaurants