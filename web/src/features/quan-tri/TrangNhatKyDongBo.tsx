import { Card } from 'antd';
import { useQuery } from '@tanstack/react-query';

import { apiTichHop } from '@/api/endpoints';
import { KhoiDangTai, KhoiLoi } from '@/components/ThanhPhanChung';
import { DaiTabTrang } from '@/components/DaiTabTrang';
import { DS_TAB_NHAT_KY } from './nhatKyTab';
import { BangNhatKyDongBo } from './TrangLienThong';

/**
 * Nhật ký đồng bộ liên thông, đặt cùng chỗ với các nhật ký khác của hệ thống.
 *
 * Bảng này vốn nằm trong một tab của màn hình Liên thông. Giữ nguyên ở đó (người đang cấu hình
 * hệ thống ngoài cần xem ngay kết quả đồng bộ) và mở thêm lối vào từ nhóm Nhật ký, vì người đi
 * tìm "hệ thống hôm qua đẩy dữ liệu có lỗi không" sẽ vào Nhật ký chứ không vào màn hình cấu hình.
 */
export default function TrangNhatKyDongBo() {
  const { data, isLoading, error, refetch } = useQuery({
    queryKey: ['he-thong-tich-hop'],
    queryFn: apiTichHop.danhSach,
  });

  if (isLoading) return <KhoiDangTai />;
  if (error) return <KhoiLoi loi={error} thuLai={refetch} />;

  return (
    <Card title="Nhật ký hệ thống">
      <DaiTabTrang danhSach={DS_TAB_NHAT_KY} dangChon="dong-bo" />

      <BangNhatKyDongBo heThong={data ?? []} />
    </Card>
  );
}
