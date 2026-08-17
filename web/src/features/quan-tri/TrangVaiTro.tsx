import { useMemo, useState } from 'react';
import { Card, Checkbox, Col, Row, Select, Table, Tag, Typography } from 'antd';
import { useQuery } from '@tanstack/react-query';

import { apiHeThong } from '@/api/endpoints';
import { KhoiDangTai, KhoiLoi } from '@/components/ThanhPhanChung';

interface VaiTro {
  id: string;
  ma: string;
  ten: string;
  moTa?: string | null;
  laHeThong: boolean;
  trangThai: number;
  quyenIds: string[];
  phamVi: { loaiPhamVi: string; donViIds: string[] }[];
}

interface Quyen {
  id: string;
  ma: string;
  ten: string;
  nhomChucNang: string;
}

/** Chức năng 45 — Ma trận phân quyền theo vai trò. */
export default function TrangVaiTro() {
  const [nhomLoc, setNhomLoc] = useState<string | undefined>();

  const { data, isLoading, error, refetch } = useQuery({
    queryKey: ['vai-tro'],
    queryFn: apiHeThong.vaiTro,
  });

  const duLieu = data as { vaiTro: VaiTro[]; quyen: Quyen[] } | undefined;

  const cacNhom = useMemo(
    () => Array.from(new Set((duLieu?.quyen ?? []).map((q) => q.nhomChucNang))),
    [duLieu],
  );

  const quyenHienThi = useMemo(
    () => (duLieu?.quyen ?? []).filter((q) => !nhomLoc || q.nhomChucNang === nhomLoc),
    [duLieu, nhomLoc],
  );

  if (isLoading) return <KhoiDangTai />;
  if (error) return <KhoiLoi loi={error} thuLai={refetch} />;

  return (
    <Card
      title="Vai trò và ma trận phân quyền"
      extra={
        <Select
          style={{ width: 220 }}
          placeholder="Tất cả nhóm chức năng"
          allowClear
          value={nhomLoc}
          options={cacNhom.map((n) => ({ value: n, label: n }))}
          onChange={setNhomLoc}
        />
      }
    >
      <Row gutter={[8, 8]} style={{ marginBottom: 16 }}>
        {(duLieu?.vaiTro ?? []).map((v) => (
          <Col key={v.id} xs={24} sm={12} lg={8}>
            <Card size="small">
              <Typography.Text strong>{v.ten}</Typography.Text>
              {v.laHeThong && <Tag color="blue" style={{ marginLeft: 8 }}>Hệ thống</Tag>}
              <div style={{ fontSize: 12, color: '#888', marginTop: 4 }}>{v.moTa}</div>
              <div style={{ marginTop: 6 }}>
                <Tag>{v.quyenIds.length} quyền</Tag>
                {v.phamVi.map((p) => (
                  <Tag key={p.loaiPhamVi} color="purple">
                    {p.loaiPhamVi.replace(/_/g, ' ')}
                  </Tag>
                ))}
              </div>
            </Card>
          </Col>
        ))}
      </Row>

      <Table<Quyen>
        rowKey="id"
        size="small"
        dataSource={quyenHienThi}
        scroll={{ x: 200 + (duLieu?.vaiTro.length ?? 0) * 120 }}
        pagination={{ pageSize: 30, showSizeChanger: true }}
        columns={[
          {
            title: 'Chức năng',
            dataIndex: 'ten',
            width: 260,
            fixed: 'left',
            render: (v: string, dong) => (
              <div>
                <div>{v}</div>
                <Typography.Text type="secondary" style={{ fontSize: 11 }}>
                  {dong.ma}
                </Typography.Text>
              </div>
            ),
          },
          { title: 'Nhóm', dataIndex: 'nhomChucNang', width: 130, responsive: ['lg'] },
          ...(duLieu?.vaiTro ?? []).map((v) => ({
            title: v.ten,
            key: v.id,
            width: 130,
            align: 'center' as const,
            render: (_giaTri: unknown, dong: Quyen) => (
              <Checkbox checked={v.quyenIds.includes(dong.id)} disabled />
            ),
          })),
        ]}
      />

      <Typography.Paragraph type="secondary" style={{ marginTop: 12, fontSize: 12 }}>
        Ma trận hiển thị ở chế độ chỉ đọc. Việc thay đổi phân quyền được thực hiện qua API
        cấu hình vai trò và luôn được ghi vào nhật ký hệ thống.
      </Typography.Paragraph>
    </Card>
  );
}
