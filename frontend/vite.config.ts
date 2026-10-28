import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

// in dev the API runs on 5080, vite proxies to it so there's no CORS setup
export default defineConfig({
  plugins: [react()],
  server: {
    port: 5173,
    proxy: {
      '/api': 'http://localhost:5080',
      '/statement': 'http://localhost:5080',
    },
  },
})
