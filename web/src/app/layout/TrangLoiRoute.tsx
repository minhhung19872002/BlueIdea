import { isRouteErrorResponse, useNavigate, useRouteError } from 'react-router-dom';
import { Button, Result, Space, Typography } from 'antd';

/**
 * Trang hiển thị khi một route gặp lỗi hiển thị.
 * Người dùng cuối chỉ thấy thông báo tiếng Việt; chi tiết kỹ thuật chỉ hiện khi chạy dev.
 */
export function TrangLoiRoute() {
  const loi = useRouteError();
  const dieuHuong = useNavigate();

  const laLoiRoute = isRouteErrorResponse(loi);
  const tieuDe = laLoiRoute && loi.status === 404 ? 'Không tìm thấy trang' : 'Đã xảy ra lỗi';

  const chiTiet =
    loi instanceof Error ? loi.message : laLoiRoute ? loi.statusText : String(loi ?? '');

  return (
    <Result
      status={laLoiRoute && loi.status === 404 ? '404' : 'error'}
      title={tieuDe}
      subTitle="Vui lòng thử lại. Nếu lỗi tiếp diễn, hãy liên hệ quản trị viên hệ thống."
      extra={
        <Space direction="vertical" align="center">
          <Space>
            <Button type="primary" onClick={() => dieuHuong('/')}>
              Về trang chủ
            </Button>
            <Button onClick={() => window.location.reload()}>Tải lại trang</Button>
          </Space>

          {import.meta.env.DEV && chiTiet && (
            <Typography.Text type="secondary" code style={{ fontSize: 12 }}>
              {chiTiet}
            </Typography.Text>
          )}
        </Space>
      }
    />
  );
}
