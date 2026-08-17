import { useState } from 'react';
import { App, Button, Card, Input, Select, Space, Table, Tag } from 'antd';
import { LockOutlined, UnlockOutlined } from '@ant-design/icons';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';

import { guiDuLieu, http, LoiApi } from '@/api/client';
import { apiDonVi, apiHeThong } from '@/api/endpoints';
import { KhoiLoi, ngayGio } from '@/components/ThanhPhanChung';

interface DongNguoiDung {
  id: string;
  tenDangNhap: string;
  hoTen: string;
  email?: string | null;
  dienThoai?: string | null;
  chucVu?: string | null;
  donViId?: string | null;
  trangThaiTaiKhoan: string;
  lanDangNhapCuoi?: string | null;
  mfaEnabled: boolean;
}

/** Chức năng 43 — Quản lý người dùng. */
export default function TrangNguoiDung() {
  const { message } = App.useApp();
  const queryClient = useQueryClient();

  const [trang, setTrang] = useState(1);
  const [soDong, setSoDong] = useState(20);
  const [tuKhoa, setTuKhoa] = useState('');
  const [donViId, setDonViId] = useState<string | undefined>();

  const { data, isLoading, error, refetch } = useQuery({
    queryKey: ['nguoi-dung', { trang, soDong, tuKhoa, donViId }],
    queryFn: () => apiHeThong.nguoiDung({ trang, soDong, tuKhoa, donViId }),
  });

  const { data: cacDonVi } = useQuery({ queryKey: ['don-vi-chon'], queryFn: apiDonVi.chon });

  const doiTrangThai = useMutation({
    mutationFn: ({ id, trangThai }: { id: string; trangThai: string }) =>
      http.patch(`/api/v1/he-thong/nguoi-dung/${id}/trang-thai?trangThai=${trangThai}`),
    onSuccess: () => {
      message.success('Đã cập nhật trạng thái tài khoản');
      void queryClient.invalidateQueries({ queryKey: ['nguoi-dung'] });
    },
    onError: (loi) => message.error(loi instanceof LoiApi ? loi.message : 'Không cập nhật được.'),
  });

  if (error) return <KhoiLoi loi={error} thuLai={refetch} />;

  const duLieu = (data?.duLieu ?? []) as unknown as DongNguoiDung[];

  return (
    <Card
      title="Quản lý người dùng"
      extra={
        <Space>
          <Input.Search
            placeholder="Tìm theo họ tên hoặc tài khoản"
            allowClear
            style={{ width: 260 }}
            onSearch={(v) => {
              setTuKhoa(v);
              setTrang(1);
            }}
          />
          <Select
            style={{ width: 220 }}
            placeholder="Tất cả đơn vị"
            allowClear
            showSearch
            optionFilterProp="label"
            value={donViId}
            options={(cacDonVi ?? []).map((x) => ({ value: x.id, label: x.ten }))}
            onChange={setDonViId}
          />
        </Space>
      }
    >
      <Table<DongNguoiDung>
        rowKey="id"
        size="middle"
        loading={isLoading}
        dataSource={duLieu}
        scroll={{ x: 1000 }}
        columns={[
          { title: 'Tài khoản', dataIndex: 'tenDangNhap', width: 160 },
          { title: 'Họ và tên', dataIndex: 'hoTen', width: 220 },
          { title: 'Chức vụ', dataIndex: 'chucVu', responsive: ['lg'] },
          { title: 'Email', dataIndex: 'email', width: 240, responsive: ['xl'] },
          {
            title: 'Trạng thái',
            dataIndex: 'trangThaiTaiKhoan',
            width: 150,
            render: (v: string) => {
              const bang: Record<string, { mau: string; ten: string }> = {
                HOAT_DONG: { mau: 'success', ten: 'Hoạt động' },
                KHOA: { mau: 'error', ten: 'Đã khóa' },
                CHO_KICH_HOAT: { mau: 'warning', ten: 'Chờ kích hoạt' },
              };
              const m = bang[v] ?? { mau: 'default', ten: v };
              return <Tag color={m.mau}>{m.ten}</Tag>;
            },
          },
          {
            title: 'Đăng nhập cuối',
            dataIndex: 'lanDangNhapCuoi',
            width: 170,
            responsive: ['lg'],
            render: (v: string | null) => ngayGio(v),
          },
          {
            title: '',
            width: 130,
            fixed: 'right',
            render: (_v, dong) =>
              dong.trangThaiTaiKhoan === 'HOAT_DONG' ? (
                <Button
                  size="small"
                  danger
                  icon={<LockOutlined />}
                  onClick={() => doiTrangThai.mutate({ id: dong.id, trangThai: 'KHOA' })}
                >
                  Khóa
                </Button>
              ) : (
                <Button
                  size="small"
                  icon={<UnlockOutlined />}
                  onClick={() => doiTrangThai.mutate({ id: dong.id, trangThai: 'HOAT_DONG' })}
                >
                  Mở khóa
                </Button>
              ),
          },
        ]}
        pagination={{
          current: trang,
          pageSize: soDong,
          total: data?.tongSo ?? 0,
          showSizeChanger: true,
          showTotal: (t) => `Tổng ${t} người dùng`,
          onChange: (t, s) => {
            setTrang(t);
            setSoDong(s);
          },
        }}
      />
    </Card>
  );
}

export { guiDuLieu };
