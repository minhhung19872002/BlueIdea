import React from 'react';
import ReactDOM from 'react-dom/client';
import { RouterProvider } from 'react-router-dom';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { App as AntApp, ConfigProvider } from 'antd';
import viVN from 'antd/locale/vi_VN';
import dayjs from 'dayjs';
import 'dayjs/locale/vi';
import utc from 'dayjs/plugin/utc';
import timezone from 'dayjs/plugin/timezone';
import relativeTime from 'dayjs/plugin/relativeTime';

import { taoChuDe } from '@/app/giaoDien';
import { router } from '@/app/router';
import { useCauHinhStore } from '@/app/store/cauHinhStore';
import './styles/global.css';
import './styles/thiet-ke.css';
import './styles/cong-khai.css';

dayjs.extend(utc);
dayjs.extend(timezone);
dayjs.extend(relativeTime);
dayjs.locale('vi');
dayjs.tz.setDefault('Asia/Ho_Chi_Minh');

const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      staleTime: 30_000,
      retry: 1,
      refetchOnWindowFocus: false,
    },
  },
});

function Goc() {
  // Màu chủ đạo đọc từ cấu hình hệ thống (không hardcode — yêu cầu Mục 9 đặc tả).
  const mauChuDao = useCauHinhStore((s) => s.mauChuDao);

  return (
    <ConfigProvider locale={viVN} theme={taoChuDe(mauChuDao)}>
      {/*
       * QueryClientProvider phải nằm NGOÀI AntApp.
       *
       * `modal.confirm` của Ant Design dựng nội dung vào cây con của AntApp. Đặt AntApp ở ngoài
       * thì nội dung hộp thoại nằm ngoài phạm vi QueryClient, và mọi thành phần gọi `useQuery`
       * bên trong hộp thoại sẽ vỡ với lỗi "No QueryClient set" — đúng lúc người dùng vừa bấm nút.
       */}
      <QueryClientProvider client={queryClient}>
        <AntApp>
          <RouterProvider router={router} />
        </AntApp>
      </QueryClientProvider>
    </ConfigProvider>
  );
}

ReactDOM.createRoot(document.getElementById('root')!).render(
  <React.StrictMode>
    <Goc />
  </React.StrictMode>,
);
