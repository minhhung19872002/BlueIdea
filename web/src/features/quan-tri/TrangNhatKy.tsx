import { useState } from 'react';
import { useParams } from 'react-router-dom';
import { Card, DatePicker, Input, Select, Space, Table, Tag, Typography } from 'antd';
import { useQuery } from '@tanstack/react-query';

import { apiHeThong } from '@/api/endpoints';
import { KhoiLoi, ngayGio } from '@/components/ThanhPhanChung';
import { DaiTabTrang } from '@/components/DaiTabTrang';

interface DongNhatKyHeThong {
  id: string;
  tenDangNhap?: string | null;
  hanhDong: string;
  module: string;
  doiTuong?: string | null;
  moTa?: string | null;
  diaChiIp?: string | null;
  ketQua: string;
  thoiGian: string;
  duLieuTruoc?: string | null;
  duLieuSau?: string | null;
}

interface DongNhatKyDangNhap {
  id: string;
  tenDangNhap: string;
  thanhCong: boolean;
  lyDoThatBai?: string | null;
  diaChiIp?: string | null;
  userAgent?: string | null;
  thoiGian: string;
}

/** Các nhánh con của trang — thiết kế gộp thành một mục ở thanh điều hướng. */
const DS_TAB = [
  { ma: 'he-thong', ten: 'Hệ thống', duongDan: '/quan-tri/nhat-ky/he-thong' },
  { ma: 'dang-nhap', ten: 'Đăng nhập', duongDan: '/quan-tri/nhat-ky/dang-nhap' },
  { ma: 'loi', ten: 'Lỗi hệ thống', duongDan: '/quan-tri/nhat-ky-loi' },
];

/** Nhật ký hệ thống và nhật ký đăng nhập. */
export default function TrangNhatKy() {
  const { loai = 'he-thong' } = useParams<{ loai: string }>();
  const laDangNhap = loai === 'dang-nhap';

  const [trang, setTrang] = useState(1);
  const [soDong, setSoDong] = useState(20);
  const [module, setModule] = useState<string | undefined>();
  const [tenDangNhap, setTenDangNhap] = useState<string | undefined>();
  const [khoang, setKhoang] = useState<[string?, string?]>([]);

  const thamSo = laDangNhap
    ? { trang, soDong, tenDangNhap }
    : { trang, soDong, module, tuNgay: khoang[0], denNgay: khoang[1] };

  const { data, isLoading, error, refetch } = useQuery({
    queryKey: ['nhat-ky', loai, thamSo],
    queryFn: () =>
      laDangNhap ? apiHeThong.nhatKyDangNhap(thamSo) : apiHeThong.nhatKyHeThong(thamSo),
  });

  if (error) return <KhoiLoi loi={error} thuLai={refetch} />;

  return (
    <Card
      title="Nhật ký hệ thống"
      extra={
        <Space wrap>
          {laDangNhap ? (
            <Input.Search
              placeholder="Tên đăng nhập"
              allowClear
              style={{ width: 220 }}
              onSearch={(v) => {
                setTenDangNhap(v || undefined);
                setTrang(1);
              }}
            />
          ) : (
            <>
              <Select
                style={{ width: 180 }}
                placeholder="Tất cả module"
                allowClear
                value={module}
                options={[
                  'SANG_KIEN',
                  'XU_LY',
                  'DANH_GIA',
                  'XAC_THUC',
                  'QUAN_TRI',
                  'DANH_MUC',
                ].map((m) => ({ value: m, label: m.replace(/_/g, ' ') }))}
                onChange={setModule}
              />
              <DatePicker.RangePicker
                format="DD/MM/YYYY"
                onChange={(k) =>
                  setKhoang([k?.[0]?.toISOString(), k?.[1]?.toISOString()])
                }
              />
            </>
          )}
        </Space>
      }
    >
      <DaiTabTrang danhSach={DS_TAB} dangChon={loai} />

      {laDangNhap ? (
        <Table<DongNhatKyDangNhap>
          rowKey="id"
          size="small"
          loading={isLoading}
          dataSource={(data?.duLieu ?? []) as unknown as DongNhatKyDangNhap[]}
          scroll={{ x: 800 }}
          columns={[
            { title: 'Tài khoản', dataIndex: 'tenDangNhap', width: 180 },
            {
              title: 'Kết quả',
              dataIndex: 'thanhCong',
              width: 120,
              render: (v: boolean) =>
                v ? <Tag color="success">Thành công</Tag> : <Tag color="error">Thất bại</Tag>,
            },
            { title: 'Lý do', dataIndex: 'lyDoThatBai' },
            { title: 'Địa chỉ IP', dataIndex: 'diaChiIp', width: 160 },
            {
              title: 'Thời gian',
              dataIndex: 'thoiGian',
              width: 170,
              render: (v: string) => ngayGio(v),
            },
          ]}
          pagination={{
            current: trang,
            pageSize: soDong,
            total: data?.tongSo ?? 0,
            showSizeChanger: true,
            onChange: (t, s) => {
              setTrang(t);
              setSoDong(s);
            },
          }}
        />
      ) : (
        <Table<DongNhatKyHeThong>
          rowKey="id"
          size="small"
          loading={isLoading}
          dataSource={(data?.duLieu ?? []) as unknown as DongNhatKyHeThong[]}
          scroll={{ x: 1000 }}
          expandable={{
            rowExpandable: (dong) => !!dong.duLieuTruoc || !!dong.duLieuSau,
            expandedRowRender: (dong) => (
              <Space direction="vertical" style={{ width: '100%' }}>
                {dong.duLieuTruoc && (
                  <div>
                    <Typography.Text strong>Dữ liệu trước:</Typography.Text>
                    <pre style={{ fontSize: 12, whiteSpace: 'pre-wrap' }}>{dong.duLieuTruoc}</pre>
                  </div>
                )}
                {dong.duLieuSau && (
                  <div>
                    <Typography.Text strong>Dữ liệu sau:</Typography.Text>
                    <pre style={{ fontSize: 12, whiteSpace: 'pre-wrap' }}>{dong.duLieuSau}</pre>
                  </div>
                )}
              </Space>
            ),
          }}
          columns={[
            { title: 'Tài khoản', dataIndex: 'tenDangNhap', width: 150 },
            { title: 'Module', dataIndex: 'module', width: 140 },
            { title: 'Hành động', dataIndex: 'hanhDong', width: 150 },
            { title: 'Đối tượng', dataIndex: 'doiTuong', width: 180, responsive: ['lg'] },
            { title: 'Mô tả', dataIndex: 'moTa', responsive: ['xl'] },
            {
              title: 'Kết quả',
              dataIndex: 'ketQua',
              width: 120,
              render: (v: string) =>
                v === 'THANH_CONG' ? (
                  <Tag color="success">Thành công</Tag>
                ) : (
                  <Tag color="error">Thất bại</Tag>
                ),
            },
            { title: 'IP', dataIndex: 'diaChiIp', width: 140, responsive: ['lg'] },
            {
              title: 'Thời gian',
              dataIndex: 'thoiGian',
              width: 170,
              render: (v: string) => ngayGio(v),
            },
          ]}
          pagination={{
            current: trang,
            pageSize: soDong,
            total: data?.tongSo ?? 0,
            showSizeChanger: true,
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
