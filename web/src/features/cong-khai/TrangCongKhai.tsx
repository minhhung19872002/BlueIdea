import { useEffect, useState } from 'react';
import { Card, Col, Input, Layout, Row, Table, Tag, Typography } from 'antd';
import { useQuery } from '@tanstack/react-query';

import { layPhanTrang } from '@/api/client';
import type { SangKienTomTat } from '@/api/endpoints';
import { useCauHinhStore } from '@/app/store/cauHinhStore';
import { KhoiRong, ngayGio } from '@/components/ThanhPhanChung';

/**
 * Trang tra cứu công khai — KHÔNG cần đăng nhập.
 * Chỉ hiển thị sáng kiến đã được công nhận và được đánh dấu công khai.
 */
export default function TrangCongKhai() {
  const [tuKhoa, setTuKhoa] = useState('');
  const [trang, setTrang] = useState(1);
  const [soDong, setSoDong] = useState(20);

  const { tenHeThong, tenDonVi, napCauHinhCongKhai } = useCauHinhStore();

  useEffect(() => {
    void napCauHinhCongKhai();
  }, [napCauHinhCongKhai]);

  const { data, isLoading } = useQuery({
    queryKey: ['cong-khai', { tuKhoa, trang, soDong }],
    queryFn: () =>
      layPhanTrang<SangKienTomTat>('/api/v1/sang-kien', {
        tuKhoa,
        trang,
        soDong,
        chiCongKhai: true,
      }),
  });

  return (
    <Layout style={{ minHeight: '100vh', background: '#f0f2f5' }}>
      <Layout.Header style={{ background: '#fff', borderBottom: '1px solid #f0f0f0' }}>
        <Typography.Text strong style={{ fontSize: 16 }}>
          {tenHeThong}
        </Typography.Text>
        {tenDonVi && (
          <Typography.Text type="secondary" style={{ marginLeft: 8 }}>
            — {tenDonVi}
          </Typography.Text>
        )}
      </Layout.Header>

      <Layout.Content className="noi-dung-trang">
        <Card title="Tra cứu sáng kiến đã được công nhận">
          <Row gutter={[8, 8]} style={{ marginBottom: 12 }}>
            <Col xs={24} md={12}>
              <Input.Search
                size="large"
                placeholder="Nhập tên sáng kiến, đơn vị hoặc tác giả"
                allowClear
                onSearch={(v) => {
                  setTuKhoa(v);
                  setTrang(1);
                }}
              />
            </Col>
          </Row>

          {!isLoading && (data?.tongSo ?? 0) === 0 ? (
            <KhoiRong moTa="Chưa có sáng kiến công khai nào khớp từ khóa tìm kiếm." />
          ) : (
            <Table<SangKienTomTat>
              rowKey="id"
              size="middle"
              loading={isLoading}
              dataSource={data?.duLieu ?? []}
              scroll={{ x: 800 }}
              columns={[
                { title: 'Mã hồ sơ', dataIndex: 'maHoSo', width: 150 },
                { title: 'Tên sáng kiến', dataIndex: 'tenSangKien' },
                { title: 'Tác giả', dataIndex: 'tacGiaChinh', width: 200 },
                { title: 'Đơn vị', dataIndex: 'tenDonVi', width: 220, responsive: ['lg'] },
                { title: 'Lĩnh vực', dataIndex: 'tenLinhVuc', width: 170, responsive: ['xl'] },
                {
                  title: 'Kết quả',
                  dataIndex: 'ketQua',
                  width: 140,
                  render: () => <Tag color="success">Được công nhận</Tag>,
                },
                {
                  title: 'Ngày nộp',
                  dataIndex: 'ngayNop',
                  width: 130,
                  render: (v: string | null) => ngayGio(v, false),
                },
              ]}
              pagination={{
                current: trang,
                pageSize: soDong,
                total: data?.tongSo ?? 0,
                showSizeChanger: true,
                showTotal: (t) => `Tổng ${t} sáng kiến`,
                onChange: (t, s) => {
                  setTrang(t);
                  setSoDong(s);
                },
              }}
            />
          )}
        </Card>
      </Layout.Content>

      <Layout.Footer style={{ textAlign: 'center', color: '#888' }}>
        {tenDonVi} — Trang tra cứu công khai
      </Layout.Footer>
    </Layout>
  );
}
