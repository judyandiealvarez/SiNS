import { fileURLToPath, URL } from 'node:url'
import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'

export default defineConfig({
  plugins: [vue()],
  resolve: {
    alias: {
      '@': fileURLToPath(new URL('./src', import.meta.url))
    }
  },
  build: {
    outDir: fileURLToPath(new URL('../sins/wwwroot', import.meta.url)),
    emptyOutDir: true
  },
  test: {
    environment: 'jsdom',
    globals: true
  },
  server: {
    proxy: {
      '/api': {
        target: process.env.VITE_API_PROXY_TARGET?.trim() || 'http://127.0.0.1:5000',
        changeOrigin: true
      },
      '/connect': {
        target: process.env.VITE_API_PROXY_TARGET?.trim() || 'http://127.0.0.1:5000',
        changeOrigin: true
      },
      '/.well-known': {
        target: process.env.VITE_API_PROXY_TARGET?.trim() || 'http://127.0.0.1:5000',
        changeOrigin: true
      }
    }
  }
})
