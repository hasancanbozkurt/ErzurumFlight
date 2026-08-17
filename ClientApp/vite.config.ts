import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

// https://vite.dev/config/
// Dev sırasında /api ve /hubs isteklerini ASP.NET Core backend'e (varsayılan https://localhost:5001) yönlendirir.
export default defineConfig({
  plugins: [react()],
  server: {
    port: 5173,
    proxy: {
      '/api': {
        target: 'https://localhost:5001',
        changeOrigin: true,
        secure: false
      },
      '/hubs': {
        target: 'https://localhost:5001',
        changeOrigin: true,
        secure: false,
        ws: true
      }
    }
  }
})
