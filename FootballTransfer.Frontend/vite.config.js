import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

// https://vite.dev/config/
export default defineConfig({
  plugins: [react()],
  test: {
    environment: 'jsdom',
    setupFiles: ['./src/test/setup.js'],
    coverage: {
      provider: 'v8',
      reporter: ['text', 'json-summary'],
      include: ['src/api/**/*.{js,jsx}', 'src/pages/**/*.{js,jsx}'],
      exclude: ['src/**/*.test.{js,jsx}'],
      thresholds: {
        lines: 25,
        functions: 25,
        statements: 25,
        branches: 20,
      },
    },
  },
})
