import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

// https://vite.dev/config/
export default defineConfig({
  plugins: [react()],
  server: {
    host: true,
    port: 5175,
    proxy: {
      '/api': {
        // En Docker Compose apunta al servicio "backend"; en desarrollo local sin Docker usa localhost:8006.
        target: process.env.VITE_API_PROXY_TARGET ?? 'http://localhost:8006',
        changeOrigin: true,
      },
    },
  },
})
