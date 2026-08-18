import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { AutoComplete, Input, Space, Tag, Typography } from 'antd';
import { SearchOutlined } from '@ant-design/icons';
import { useQuery } from '@tanstack/react-query';

import { apiSangKien } from '@/api/endpoints';
import { useAuthStore } from '@/app/store/authStore';

const NHAN_LOAI: Record<string, string> = {
  MA_HO_SO: 'Mã hồ sơ',
  SANG_KIEN: 'Sáng kiến',
  TAC_GIA: 'Tác giả',
};

/**
 * Ô tìm kiếm nhanh trên thanh tiêu đề.
 *
 * Gõ mã hồ sơ rồi Enter là vào thẳng màn hình tra cứu đã lọc sẵn — người tiếp nhận và cán bộ xử
 * lý tra hồ sơ hàng chục lần mỗi ngày, bắt họ mở menu rồi mới gõ là thừa hai thao tác mỗi lần.
 *
 * Gợi ý chạy trên đúng phạm vi dữ liệu người dùng được xem (máy chủ lọc), nên ô này không làm lộ
 * hồ sơ của đơn vị khác.
 */
export function TimKiemNhanh() {
  const dieuHuong = useNavigate();
  const [tuKhoa, setTuKhoa] = useState('');

  // Tài khoản không có quyền xem danh sách hồ sơ thì ô này vô nghĩa — ẩn hẳn thay vì để người
  // dùng gõ vào rồi nhận màn hình báo thiếu quyền.
  const duocXem = useAuthStore((s) => s.coQuyen('SANG_KIEN.XEM'));

  const { data: goiY } = useQuery({
    queryKey: ['goi-y-tim-kiem', tuKhoa],
    queryFn: () => apiSangKien.goiY(tuKhoa, 6),
    enabled: duocXem && tuKhoa.trim().length >= 2,
    staleTime: 30_000,
  });

  if (!duocXem) return null;

  function tim(giaTri: string) {
    const v = giaTri.trim();
    if (!v) return;

    dieuHuong(`/tra-cuu?tuKhoa=${encodeURIComponent(v)}`);
    setTuKhoa('');
  }

  return (
    <AutoComplete
      value={tuKhoa}
      onChange={setTuKhoa}
      onSelect={tim}
      style={{ width: 260 }}
      options={(goiY ?? []).map((x) => ({
        value: x.giaTri,
        label: (
          <Space size={6}>
            <Tag color={x.loai === 'TAC_GIA' ? 'purple' : 'blue'}>
              {NHAN_LOAI[x.loai] ?? x.loai}
            </Tag>
            <Typography.Text ellipsis style={{ maxWidth: 150 }}>
              {x.giaTri}
            </Typography.Text>
          </Space>
        ),
      }))}
    >
      <Input
        allowClear
        prefix={<SearchOutlined />}
        placeholder="Tìm nhanh mã hồ sơ, sáng kiến…"
        aria-label="Tìm kiếm nhanh"
        onPressEnter={(e) => tim((e.target as HTMLInputElement).value)}
      />
    </AutoComplete>
  );
}
