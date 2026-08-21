import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';
import path from 'node:path';

export default defineConfig({
  plugins: [react()],
  resolve: {
    alias: { '@': path.resolve(__dirname, './src') },
  },
  server: {
    port: Number(process.env.WEB_PORT) || 5173,
    proxy: {
      /*
       * Trong môi trường phát triển, gọi thẳng API qua proxy để tránh cấu hình CORS.
       *
       * Đích của proxy đọc từ `PROXY_API_URL` — biến KHÔNG có tiền tố `VITE_` nên không lọt vào
       * gói tải xuống trình duyệt. Trước đây dùng chung `VITE_API_URL` cho cả hai việc: đổi đích
       * proxy là đồng thời đổi luôn baseURL của axios thành địa chỉ tuyệt đối, và trình duyệt
       * chuyển sang gọi khác origin rồi bị CORS chặn — lỗi rất khó lần vì cấu hình "đúng như ý".
       */
      '/api': {
        target: process.env.PROXY_API_URL ?? 'http://localhost:8080',
        changeOrigin: true,
      },
      '/hubs': {
        target: process.env.PROXY_API_URL ?? 'http://localhost:8080',
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
