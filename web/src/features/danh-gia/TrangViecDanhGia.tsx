import { useState } from 'react';
import { Link } from 'react-router-dom';
import { Badge, Button, Card, Select, Space, Table, Tag, Typography } from 'antd';
import { EditOutlined } from '@ant-design/icons';
import { useQuery } from '@tanstack/react-query';
import dayjs from 'dayjs';

import { apiDanhGia, type PhanCongCham } from '@/api/endpoints';
import { KhoiLoi, KhoiRong, ngayGio } from '@/components/ThanhPhanChung';

const TRANG_THAI = [
  { value: 'CHUA_CHAM', label: 'Chưa chấm' },
  { value: 'DANG_CHAM', label: 'Đang chấm' },
  { value: 'DA_CHAM', label: 'Đã chấm' },
  { value: 'QUA_HAN', label: 'Quá hạn' },
];

/** Chức năng 33 — "Việc của tôi" cho thành viên hội đồng. */
export default function TrangViecDanhGia() {
  const [trang, setTrang] = useState(1);
  const [soDong, setSoDong] = useState(20);
  const [trangThai, setTrangThai] = useState<string | undefined>();

  const { data, isLoading, error, refetch } = useQuery({
    queryKey: ['danh-gia', 'viec-cua-toi', { trang, soDong, trangThai }],
    queryFn: () => apiDanhGia.viecCuaToi({ trang, soDong, trangThai }),
  });

  const soChuaCham = (data?.duLieu ?? []).filter(
    (x) => x.trangThaiPhanCong !== 'DA_CHAM',
  ).length;

  if (error) return <KhoiLoi loi={error} thuLai={refetch} />;

  return (
    <Card
      title={
        <Space>
          Hồ sơ được phân công chấm
          {soChuaCham > 0 && <Badge count={soChuaCham} />}
        </Space>
      }
      extra={
        <Select
          style={{ width: 180 }}
          placeholder="Tất cả trạng thái"
          allowClear
          value={trangThai}
          options={TRANG_THAI}
          onChange={setTrangThai}
        />
      }
    >
      {!isLoading && (data?.tongSo ?? 0) === 0 ? (
        <KhoiRong moTa="Bạn chưa được phân công chấm hồ sơ nào." />
      ) : (
        <Table<PhanCongCham>
          rowKey="id"
          size="middle"
          loading={isLoading}
          dataSource={data?.duLieu ?? []}
          scroll={{ x: 900 }}
          columns={[
            {
              title: 'Mã hồ sơ',
              dataIndex: 'maHoSo',
              width: 140,
              render: (giaTri: string, dong) => (
                <Link to={`/sang-kien/${dong.sangKienId}`}>{giaTri}</Link>
              ),
            },
            { title: 'Tên sáng kiến', dataIndex: 'tenSangKien' },
            { title: 'Lĩnh vực', dataIndex: 'tenLinhVuc', width: 170, responsive: ['lg'] },
            {
              title: 'Trạng thái',
              dataIndex: 'trangThaiPhanCong',
              width: 130,
              render: (giaTri: string) => {
                const bang: Record<string, { mau: string; ten: string }> = {
                  CHUA_CHAM: { mau: 'default', ten: 'Chưa chấm' },
                  DANG_CHAM: { mau: 'processing', ten: 'Đang chấm' },
                  DA_CHAM: { mau: 'success', ten: 'Đã chấm' },
                  QUA_HAN: { mau: 'error', ten: 'Quá hạn' },
                };
                const m = bang[giaTri] ?? { mau: 'default', ten: giaTri };
                return <Tag color={m.mau}>{m.ten}</Tag>;
              },
            },
            {
              title: 'Điểm đã chấm',
              dataIndex: 'tongDiemDaCham',
              width: 130,
              align: 'right',
              render: (giaTri?: number | null) => giaTri?.toFixed(2) ?? '—',
            },
            {
              title: 'Hạn hoàn thành',
              dataIndex: 'hanHoanThanh',
              width: 190,
              render: (giaTri: string | null, dong) => {
                if (!giaTri) return '—';
                const conLai = dayjs(giaTri).diff(dayjs(), 'day');
                return (
                  <Space direction="vertical" size={0}>
                    <span className={dong.quaHan ? 'qua-han' : undefined}>{ngayGio(giaTri)}</span>
                    <Typography.Text
                      type={dong.quaHan ? 'danger' : conLai <= 2 ? 'warning' : 'secondary'}
                      style={{ fontSize: 12 }}
                    >
                      {dong.quaHan ? 'Đã quá hạn' : `Còn ${conLai} ngày`}
                    </Typography.Text>
                  </Space>
                );
              },
            },
            {
              title: '',
              width: 130,
              fixed: 'right',
              render: (_giaTri, dong) => (
                <Link to={`/danh-gia/${dong.sangKienId}/cham-diem?hoiDongId=${dong.hoiDongId}`}>
                  <Button type="primary" size="small" icon={<EditOutlined />}>
                    Chấm điểm
                  </Button>
                </Link>
              ),
            },
          ]}
          pagination={{
            current: trang,
            pageSize: soDong,
            total: data?.tongSo ?? 0,
            showSizeChanger: true,
            showTotal: (t) => `Tổng ${t} hồ sơ`,
            onChange: (t, s) => {
              setTrang(t);
              setSoDong(s);
            },
          }}
        />
      )}
    </Card>
  );
}
