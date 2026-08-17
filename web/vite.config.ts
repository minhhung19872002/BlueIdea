import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';
import path from 'node:path';

export default defineConfig({
  plugins: [react()],
  resolve: {
    alias: { '@': path.resolve(__dirname, './src') },
  },
  server: {
    port: 5173,
    proxy: {
      // Trong môi trường phát triển, gọi thẳng API qua proxy để tránh cấu hình CORS.
      '/api': {
        target: process.env.VITE_API_URL ?? 'http://localhost:8080',
        changeOrigin: true,
      },
      '/hubs': {
        target: process.env.VITE_API_URL ?? 'http://localhost:8080',
        changeOrigin: true,
        ws: true,
      },
    },
  },
  build: {
    // Tách vendor để lần tải đầu < 3s (Mục 7 đặc tả).
    rollupOptions: {
      output: {
        manualChunks: {
          'vendor-react': ['react', 'react-dom', 'react-router-dom'],
          'vendor-antd': ['antd', '@ant-design/icons'],
          'vendor-chart': ['echarts', 'echarts-for-react'],
          'vendor-flow': ['reactflow'],
        },
      },
    },
    chunkSizeWarningLimit: 1200,
  },
});
