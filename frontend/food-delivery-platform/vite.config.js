import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

// https://vite.dev/config/
export default defineConfig({
    plugins: [react()],
    // Vercel serves at the domain root, so base is "/". (Was
    // "/food-delivery-platform" for the old GitHub Pages sub-path deploy.)
    base: "/"
})

