import { useState } from 'react';
import { App, Button, Card, Select, Space, Table, Tag, Typography } from 'antd';
import { CheckOutlined } from '@ant-design/icons';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';

import { LoiApi } from '@/api/client';
import { apiNhatKyLoi, type NhatKyLoi } from '@/api/endpoints';
import { KhoiLoi, KhoiRong, ngayGio } from '@/components/ThanhPhanChung';
import { DaiTabTrang } from '@/components/DaiTabTrang';

const DS_TAB = [
  { ma: 'he-thong', ten: 'Hệ thống', duongDan: '/quan-tri/nhat-ky/he-thong' },
  { ma: 'dang-nhap', ten: 'Đăng nhập', duongDan: '/quan-tri/nhat-ky/dang-nhap' },
  { ma: 'loi', ten: 'Lỗi hệ thống', duongDan: '/quan-tri/nhat-ky-loi' },
];

const MAU_MUC_DO: Record<string, string> = {
  NGHIEM_TRONG: 'error',
  LOI: 'error',
  CANH_BAO: 'warning',
};

/**
 * Nhật ký lỗi hệ thống — chỉ ghi lỗi 5xx.
 *
 * Lỗi nghiệp vụ 4xx (thiếu quyền, dữ liệu không hợp lệ) là hành vi bình thường của hệ thống,
 * đưa vào đây chỉ làm nhiễu và che mất lỗi thật cần xử lý.
 */
export default function TrangNhatKyLoi() {
  const { message } = App.useApp();
  const queryClient = useQueryClient();

  const [trang, setTrang] = useState(1);
  const [soDong, setSoDong] = useState(20);
  const [mucDo, setMucDo] = useState<string | undefined>();
  const [daXuLy, setDaXuLy] = useState<boolean | undefined>(false);

  const thamSo = { trang, soDong, mucDo, daXuLy };

  const { data, isLoading, error, refetch } = useQuery({
    queryKey: ['nhat-ky-loi', thamSo],
    queryFn: () => apiNhatKyLoi.danhSach(thamSo),
  });

  const danhDau = useMutation({
    mutationFn: (id: string) => apiNhatKyLoi.danhDauDaXuLy(id),
    onSuccess: () => {
      message.success('Đã đánh dấu lỗi đã xử lý');
      void queryClient.invalidateQueries({ queryKey: ['nhat-ky-loi'] });
    },
    onError: (loi) => message.error(loi instanceof LoiApi ? loi.message : 'Không cập nhật được.'),
  });

  if (error) return <KhoiLoi loi={error} thuLai={refetch} />;

  return (
    <Card title="Nhật ký hệ thống">
      <DaiTabTrang danhSach={DS_TAB} dangChon="loi" />

      <Space wrap style={{ marginBottom: 12 }}>
        <Select
          style={{ width: 200 }}
          allowClear
          placeholder="Mức độ"
          value={mucDo}
          options={[
            { value: 'NGHIEM_TRONG', label: 'Nghiêm trọng' },
            { value: 'LOI', label: 'Lỗi' },
            { value: 'CANH_BAO', label: 'Cảnh báo' },
          ]}
          onChange={(v) => {
            setMucDo(v);
            setTrang(1);
          }}
        />
        <Select
          style={{ width: 220 }}
          value={daXuLy}
          options={[
            { value: false, label: 'Chưa xử lý' },
            { value: true, label: 'Đã xử lý' },
            { value: undefined, label: 'Tất cả' },
          ]}
          onChange={(v) => {
            setDaXuLy(v);
            setTrang(1);
          }}
        />
      </Space>

      <Table<NhatKyLoi>
        rowKey="id"
        size="small"
        loading={isLoading}
        dataSource={data?.duLieu ?? []}
        scroll={{ x: 1100 }}
        locale={{
          emptyText: (
            <KhoiRong moTa="Không có lỗi hệ thống nào — đây là tin tốt, không phải lỗi hiển thị." />
          ),
        }}
        expandable={{
          expandedRowRender: (dong) => (
            <Typography.Paragraph
              style={{
                marginBottom: 0,
                whiteSpace: 'pre-wrap',
                fontSize: 12,
                fontFamily: 'monospace',
              }}
            >
              {dong.stackTrace ?? 'Không có stack trace.'}
            </Typography.Paragraph>
          ),
          rowExpandable: (dong) => !!dong.stackTrace,
        }}
        columns={[
          {
            title: 'Thời gian',
            dataIndex: 'thoiGian',
            width: 150,
            render: (v: string) => ngayGio(v),
          },
          {
            title: 'Mức độ',
            dataIndex: 'mucDo',
            width: 130,
            render: (v: string) => <Tag color={MAU_MUC_DO[v] ?? 'default'}>{v}</Tag>,
          },
          { title: 'Nguồn', dataIndex: 'nguon', width: 280, ellipsis: true },
          { title: 'Thông báo', dataIndex: 'thongBao', ellipsis: true },
          {
            title: 'IP',
            dataIndex: 'diaChiIp',
            width: 140,
            responsive: ['xl'],
            render: (v: string | null) => v ?? '—',
          },
          {
            title: '',
            key: 'thaoTac',
            width: 130,
            fixed: 'right',
            render: (_v, dong) =>
              dong.daXuLy ? (
                <Tag color="success">Đã xử lý</Tag>
              ) : (
                <Button
                  size="small"
                  icon={<CheckOutlined />}
                  loading={danhDau.isPending && danhDau.variables === dong.id}
                  onClick={() => danhDau.mutate(dong.id)}
                >
                  Đã xử lý
                </Button>
              ),
          },
        ]}
        pagination={{
          current: trang,
          pageSize: soDong,
          total: data?.tongSo ?? 0,
          showSizeChanger: true,
          showTotal: (t) => `Tổng ${t} lỗi`,
          onChange: (t, s) => {
            setTrang(t);
            setSoDong(s);
          },
        }}
      />
    </Card>
  );
}
