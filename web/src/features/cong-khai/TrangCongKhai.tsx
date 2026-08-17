import { useEffect, useState } from 'react';
import { Button, Input, Layout, Table } from 'antd';
import { SearchOutlined } from '@ant-design/icons';
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
  const [dangGo, setDangGo] = useState('');
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

  function tim() {
    setTuKhoa(dangGo);
    setTrang(1);
  }

  return (
    <Layout style={{ minHeight: '100vh', background: '#f0f2f5' }}>
      <Layout.Header
        style={{
          background: '#fff',
          borderBottom: '1px solid #f0f0f0',
          display: 'flex',
          alignItems: 'center',
          gap: 10,
        }}
      >
        <div
          style={{
            width: 28,
            height: 28,
            borderRadius: 7,
            background: '#1677ff',
            color: '#fff',
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center',
            fontWeight: 800,
            fontSize: 12,
            flexShrink: 0,
          }}
        >
          BI
        </div>
        <div
          style={{
            fontSize: 16,
            fontWeight: 600,
            whiteSpace: 'nowrap',
            overflow: 'hidden',
            textOverflow: 'ellipsis',
          }}
        >
          {tenHeThong}
          {tenDonVi && (
            <span style={{ color: 'rgba(0,0,0,0.45)', fontWeight: 400 }}> — {tenDonVi}</span>
          )}
        </div>
      </Layout.Header>

      <Layout.Content className="noi-dung-trang">
        {/* Banner tìm kiếm — điểm nhấn chính của cổng công khai. */}
        <div
          style={{
            background: 'linear-gradient(135deg,#1677ff 0%,#0958d9 100%)',
            borderRadius: 8,
            padding: '26px 28px',
            color: '#fff',
            marginBottom: 12,
          }}
        >
          <div style={{ fontSize: 18, fontWeight: 700 }}>
            Cổng tra cứu sáng kiến đã công nhận
          </div>
          <div style={{ fontSize: 13, opacity: 0.85, marginTop: 4 }}>
            Công khai cho người dân — không cần đăng nhập
          </div>

          <div
            style={{
              display: 'flex',
              gap: 8,
              marginTop: 14,
              maxWidth: 640,
              flexWrap: 'wrap',
            }}
          >
            <Input
              size="large"
              placeholder="Nhập tên sáng kiến, tác giả, số quyết định…"
              allowClear
              value={dangGo}
              onChange={(e) => setDangGo(e.target.value)}
              onPressEnter={tim}
              style={{ flex: '1 1 260px', minWidth: 0, borderRadius: 6 }}
            />
            <Button
              size="large"
              icon={<SearchOutlined />}
              onClick={tim}
              style={{
                // Nút dùng sắc xanh đậm hơn nền để nổi trên banner — nút primary mặc định
                // của Ant Design cùng tông với gradient nên gần như biến mất.
                background: '#003eb3',
                borderColor: '#003eb3',
                color: '#fff',
                borderRadius: 6,
                fontWeight: 600,
                flexShrink: 0,
              }}
            >
              Tìm kiếm
            </Button>
          </div>
        </div>

        <div className="tk-the tk-the-than">
          {!isLoading && (data?.tongSo ?? 0) === 0 ? (
            <KhoiRong
              moTa={
                tuKhoa
                  ? `Không tìm thấy sáng kiến công khai nào khớp “${tuKhoa}”.`
                  : 'Chưa có sáng kiến nào được công bố công khai.'
              }
            />
          ) : (
            <Table<SangKienTomTat>
              rowKey="id"
              size="middle"
              loading={isLoading}
              dataSource={data?.duLieu ?? []}
              scroll={{ x: 800 }}
              columns={[
                { title: 'Mã hồ sơ', dataIndex: 'maHoSo', width: 140 },
                { title: 'Tên sáng kiến', dataIndex: 'tenSangKien' },
                { title: 'Tác giả', dataIndex: 'tacGiaChinh', width: 180 },
                { title: 'Đơn vị', dataIndex: 'tenDonVi', width: 200, responsive: ['lg'] },
                { title: 'Lĩnh vực', dataIndex: 'tenLinhVuc', width: 160, responsive: ['xl'] },
                {
                  title: 'Năm',
                  dataIndex: 'ngayNop',
                  width: 90,
                  render: (v: string | null) => (v ? new Date(v).getFullYear() : '—'),
                },
                {
                  title: 'Ngày nộp',
                  dataIndex: 'ngayNop',
                  width: 130,
                  responsive: ['lg'],
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
        </div>
      </Layout.Content>

      <Layout.Footer style={{ textAlign: 'center', color: 'rgba(0,0,0,0.45)', fontSize: 13 }}>
        {tenDonVi} — Cổng tra cứu công khai
      </Layout.Footer>
    </Layout>
  );
}
