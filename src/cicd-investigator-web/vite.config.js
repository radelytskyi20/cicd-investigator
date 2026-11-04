import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';

export default defineConfig({
  plugins: [react()],
  server: {
    port: Number(process.env.PORT || 5173),
    strictPort: true,
    proxy: {
      '/api': { target: process.env.API_URL || 'http://localhost:5100' },
      '/investigation': { target: process.env.API_URL || 'http://localhost:5100', ws: true }
    }
  }
});
