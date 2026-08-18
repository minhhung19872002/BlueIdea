import { Component, type ErrorInfo, type ReactNode } from 'react';
import { Alert, Button, Empty, Result, Skeleton, Space, Tag, Tooltip, Typography } from 'antd';
import { ReloadOutlined, WarningOutlined } from '@ant-design/icons';
import { Link } from 'react-router-dom';
import dayjs from 'dayjs';

import { LoiApi } from '@/api/client';

/** Nhãn trạng thái hồ sơ với màu thống nhất toàn hệ thống. */
export function NhanTrangThai({ trangThai }: { trangThai?: string | null }) {
  if (!trangThai) return <Tag>—</Tag>;

  const bang: Record<string, { mau: string; ten: string }> = {
    NHAP: { mau: 'default', ten: 'Nháp' },
    DA_NOP: { mau: 'blue', ten: 'Đã nộp' },
    DANG_XU_LY: { mau: 'processing', ten: 'Đang xử lý' },
    YEU_CAU_BO_SUNG: { mau: 'orange', ten: 'Yêu cầu bổ sung' },
    DA_PHE_DUYET: { mau: 'success', ten: 'Đã phê duyệt' },
    KHONG_DAT: { mau: 'error', ten: 'Không đạt' },
    DA_RUT: { mau: 'default', ten: 'Đã rút' },
    DA_HUY: { mau: 'default', ten: 'Đã hủy' },
  };

  const muc = bang[trangThai] ?? { mau: 'default', ten: trangThai };
  return <Tag color={muc.mau}>{muc.ten}</Tag>;
}

/** Nhãn mức cảnh báo trùng lặp theo ngưỡng cấu hình. */
export function NhanTrungLap({ tyLe }: { tyLe?: number | null }) {
  if (tyLe === null || tyLe === undefined) {
    return <Tag>Chưa kiểm tra</Tag>;
  }

  const mau = tyLe > 40 ? 'error' : tyLe >= 20 ? 'warning' : 'success';
  const nhan = tyLe > 40 ? 'Nghiêm trọng' : tyLe >= 20 ? 'Cảnh báo' : 'An toàn';

  return (
    <Tooltip title={`Mức ${nhan.toLowerCase()}`}>
      <Tag color={mau}>{tyLe.toFixed(2)}%</Tag>
    </Tooltip>
  );
}

/** Hiển thị hạn xử lý, tô đỏ khi quá hạn. */
export function HienThiHan({ han, quaHan }: { han?: string | null; quaHan?: boolean }) {
  if (!han) return <span>—</span>;

  const noiDung = dayjs(han).format('DD/MM/YYYY HH:mm');

  return quaHan ? (
    <span className="qua-han">
      <WarningOutlined /> {noiDung}
    </span>
  ) : (
    <span>{noiDung}</span>
  );
}

/** Định dạng ngày giờ theo giờ Việt Nam. */
export function ngayGio(giaTri?: string | null, coGio = true) {
  if (!giaTri) return '—';
  return dayjs(giaTri).format(coGio ? 'DD/MM/YYYY HH:mm' : 'DD/MM/YYYY');
}

/** Khối hiển thị khi truy vấn lỗi, kèm nút thử lại. */
export function KhoiLoi({ loi, thuLai }: { loi: unknown; thuLai?: () => void }) {
  const thongBao =
    loi instanceof LoiApi ? loi.message : 'Đã xảy ra lỗi khi tải dữ liệu. Vui lòng thử lại.';

  return (
    <Alert
      type="error"
      showIcon
      message="Không tải được dữ liệu"
      description={thongBao}
      action={
        thuLai && (
          <Button size="small" icon={<ReloadOutlined />} onClick={thuLai}>
            Thử lại
          </Button>
        )
      }
    />
  );
}

/** Khối rỗng có hướng dẫn thay vì chỉ hiện "No data". */
export function KhoiRong({ moTa, hanhDong }: { moTa: string; hanhDong?: ReactNode }) {
  return (
    <Empty image={Empty.PRESENTED_IMAGE_SIMPLE} description={<Typography.Text type="secondary">{moTa}</Typography.Text>}>
      {hanhDong}
    </Empty>
  );
}

/**
 * Báo địa chỉ không ứng với màn hình nào.
 *
 * Vài màn hình dùng chung một route có tham số (`/quan-tri/danh-muc/:ma`, `/bao-cao/:loai`) và
 * tra tham số đó trong một bảng cấu hình. Trước đây tra không thấy thì rơi về mục mặc định, nên
 * gõ sai địa chỉ — hoặc vào một mục CHƯA CÓ MÀN HÌNH — lại hiện ra một màn hình khác trông hoàn
 * toàn bình thường. Người dùng tưởng mình đang xem đúng thứ mình chọn.
 *
 * Nêu rõ mã không nhận ra và liệt kê các mã hợp lệ, để người gõ nhầm sửa được ngay tại chỗ.
 */
export function KhoiKhongNhanRa({
  ma,
  hopLe,
  duongDanGoc,
}: {
  ma: string;
  hopLe: { ma: string; ten: string }[];
  duongDanGoc: string;
}) {
  return (
    <Result
      status="404"
      title="Không có màn hình cho mục này"
      subTitle={`Địa chỉ chứa mã "${ma}" nhưng hệ thống không có mục nào mang mã đó.`}
      extra={
        <Space direction="vertical" align="center">
          <Typography.Text type="secondary">Các mục hiện có:</Typography.Text>
          <Space wrap>
            {hopLe.map((x) => (
              <Link key={x.ma} to={`${duongDanGoc}/${x.ma}`}>
                <Button size="small">{x.ten}</Button>
              </Link>
            ))}
          </Space>
        </Space>
      }
    />
  );
}

/** Skeleton dùng chung khi đang tải trang. */
export function KhoiDangTai({ soDong = 6 }: { soDong?: number }) {
  return (
    <div className="the-trang">
      <Skeleton active paragraph={{ rows: soDong }} />
    </div>
  );
}

interface TrangThaiRanhGioi {
  coLoi: boolean;
  thongBao?: string;
}

/** Error boundary chặn lỗi render làm sập toàn bộ ứng dụng. */
export class RanhGioiLoi extends Component<{ children: ReactNode }, TrangThaiRanhGioi> {
  constructor(props: { children: ReactNode }) {
    super(props);
    this.state = { coLoi: false };
  }

  static getDerivedStateFromError(loi: Error): TrangThaiRanhGioi {
    return { coLoi: true, thongBao: loi.message };
  }

  componentDidCatch(loi: Error, thongTin: ErrorInfo) {
    // Ghi ra console để lập trình viên điều tra; production nên đẩy về máy chủ log.
    console.error('Lỗi hiển thị:', loi, thongTin.componentStack);
  }

  render() {
    if (this.state.coLoi) {
      return (
        <Result
          status="error"
          title="Giao diện gặp sự cố"
          subTitle={this.state.thongBao}
          extra={
            <Button type="primary" onClick={() => window.location.reload()}>
              Tải lại trang
            </Button>
          }
        />
      );
    }

    return this.props.children;
  }
}
