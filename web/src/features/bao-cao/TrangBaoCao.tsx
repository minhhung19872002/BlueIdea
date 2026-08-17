import { useMemo, useState } from 'react';
import { useParams } from 'react-router-dom';
import { Button, Card, Col, Row, Select, Space, Table } from 'antd';
import { FileExcelOutlined, FilePdfOutlined } from '@ant-design/icons';
import { useQuery } from '@tanstack/react-query';

import { taiTep } from '@/api/client';
import {
  apiBaoCao,
  apiDonVi,
  apiDotDeNghi,
  apiLinhVuc,
  type DongBaoCaoDonVi,
  type DongBaoCaoSangKien,
} from '@/api/endpoints';
import { KhoiLoi, KhoiRong, ngayGio } from '@/components/ThanhPhanChung';

const CAU_HINH_BAO_CAO: Record<
  string,
  { tieuDe: string; duongDanXuat: string; tenTep: string; laTheoDonVi?: boolean }
> = {
  'sang-kien-dat': {
    tieuDe: 'Danh sách sáng kiến được công nhận',
    duongDanXuat: '/api/v1/bao-cao/sang-kien-dat/xuat-excel',
    tenTep: 'sang-kien-dat.xlsx',
  },
  'sang-kien-chua-dat': {
    tieuDe: 'Danh sách sáng kiến chưa đạt',
    duongDanXuat: '/api/v1/bao-cao/sang-kien-chua-dat/xuat-excel',
    tenTep: 'sang-kien-chua-dat.xlsx',
  },
  'theo-don-vi': {
    tieuDe: 'Thống kê sáng kiến theo đơn vị',
    duongDanXuat: '/api/v1/bao-cao/theo-don-vi/xuat-excel',
    tenTep: 'thong-ke-theo-don-vi.xlsx',
    laTheoDonVi: true,
  },
  'ket-qua': {
    tieuDe: 'Kết quả xét sáng kiến',
    duongDanXuat: '/api/v1/bao-cao/sang-kien-dat/xuat-excel',
    tenTep: 'ket-qua-sang-kien.xlsx',
  },
};

/** Chức năng 38–40 — Các báo cáo bắt buộc. */
export default function TrangBaoCao() {
  const { loai = 'sang-kien-dat' } = useParams<{ loai: string }>();
  const cauHinh = CAU_HINH_BAO_CAO[loai] ?? CAU_HINH_BAO_CAO['sang-kien-dat'];

  const [dotDeNghiId, setDotDeNghiId] = useState<string | undefined>();
  const [linhVucId, setLinhVucId] = useState<string | undefined>();
  const [donViId, setDonViId] = useState<string | undefined>();

  const thamSo = useMemo(
    () => ({ dotDeNghiId, linhVucId, donViId }),
    [dotDeNghiId, linhVucId, donViId],
  );

  const { data: cacDot } = useQuery({ queryKey: ['dot-chon'], queryFn: apiDotDeNghi.chon });
  const { data: cacLinhVuc } = useQuery({ queryKey: ['linh-vuc-chon'], queryFn: apiLinhVuc.chon });
  const { data: cacDonVi } = useQuery({ queryKey: ['don-vi-chon'], queryFn: apiDonVi.chon });

  const truyVan = useQuery<Array<DongBaoCaoSangKien | DongBaoCaoDonVi>>({
    queryKey: ['bao-cao', loai, thamSo],
    queryFn: () => {
      if (loai === 'theo-don-vi') return apiBaoCao.theoDonVi(thamSo);
      if (loai === 'sang-kien-chua-dat') return apiBaoCao.sangKienChuaDat(thamSo);
      return apiBaoCao.sangKienDat(thamSo);
    },
  });

  if (truyVan.error) return <KhoiLoi loi={truyVan.error} thuLai={truyVan.refetch} />;

  const duLieu = truyVan.data ?? [];

  return (
    <Card
      title={cauHinh.tieuDe}
      extra={
        <Space className="khong-in">
          <Button
            icon={<FileExcelOutlined />}
            onClick={() => taiTep(cauHinh.duongDanXuat, cauHinh.tenTep, thamSo)}
          >
            Xuất Excel
          </Button>
          {!cauHinh.laTheoDonVi && (
            <Button
              icon={<FilePdfOutlined />}
              onClick={() =>
                taiTep('/api/v1/bao-cao/sang-kien-dat/xuat-pdf', 'bao-cao.pdf', thamSo)
              }
            >
              Xuất PDF
            </Button>
          )}
          <Button onClick={() => window.print()}>In</Button>
        </Space>
      }
    >
      <Row gutter={[8, 8]} style={{ marginBottom: 12 }} className="khong-in">
        <Col xs={24} md={8}>
          <Select
            style={{ width: '100%' }}
            placeholder="Tất cả đợt"
            allowClear
            value={dotDeNghiId}
            options={(cacDot ?? []).map((x) => ({ value: x.id, label: x.ten }))}
            onChange={setDotDeNghiId}
          />
        </Col>
        <Col xs={24} md={8}>
          <Select
            style={{ width: '100%' }}
            placeholder="Tất cả lĩnh vực"
            allowClear
            value={linhVucId}
            options={(cacLinhVuc ?? []).map((x) => ({ value: x.id, label: x.ten }))}
            onChange={setLinhVucId}
          />
        </Col>
        <Col xs={24} md={8}>
          <Select
            style={{ width: '100%' }}
            placeholder="Tất cả đơn vị"
            allowClear
            showSearch
            optionFilterProp="label"
            value={donViId}
            options={(cacDonVi ?? []).map((x) => ({ value: x.id, label: x.ten }))}
            onChange={setDonViId}
          />
        </Col>
      </Row>

      {!truyVan.isLoading && duLieu.length === 0 ? (
        <KhoiRong moTa="Chưa có dữ liệu phù hợp với bộ lọc đã chọn." />
      ) : cauHinh.laTheoDonVi ? (
        <Table<DongBaoCaoDonVi>
          rowKey="maDonVi"
          size="middle"
          loading={truyVan.isLoading}
          dataSource={duLieu as DongBaoCaoDonVi[]}
          scroll={{ x: 800 }}
          summary={(dong) => {
            const tong = dong.reduce((s, x) => s + x.tongSo, 0);
            const dat = dong.reduce((s, x) => s + x.soDat, 0);
            return (
              <Table.Summary.Row>
                <Table.Summary.Cell index={0} colSpan={2}>
                  <strong>Tổng cộng</strong>
                </Table.Summary.Cell>
                <Table.Summary.Cell index={2} align="right">
                  <strong>{tong}</strong>
                </Table.Summary.Cell>
                <Table.Summary.Cell index={3} align="right">
                  <strong>{dat}</strong>
                </Table.Summary.Cell>
                <Table.Summary.Cell index={4} colSpan={3} align="right">
                  <strong>{tong === 0 ? 0 : ((dat / tong) * 100).toFixed(2)}%</strong>
                </Table.Summary.Cell>
              </Table.Summary.Row>
            );
          }}
          columns={[
            { title: 'Mã đơn vị', dataIndex: 'maDonVi', width: 160 },
            { title: 'Tên đơn vị', dataIndex: 'tenDonVi' },
            { title: 'Tổng hồ sơ', dataIndex: 'tongSo', width: 120, align: 'right' },
            { title: 'Đạt', dataIndex: 'soDat', width: 90, align: 'right' },
            { title: 'Không đạt', dataIndex: 'soKhongDat', width: 120, align: 'right' },
            { title: 'Đang xử lý', dataIndex: 'soDangXuLy', width: 120, align: 'right' },
            {
              title: 'Tỷ lệ đạt',
              dataIndex: 'tyLeDat',
              width: 110,
              align: 'right',
              sorter: (a, b) => a.tyLeDat - b.tyLeDat,
              render: (v: number) => `${v.toFixed(2)}%`,
            },
          ]}
          pagination={false}
        />
      ) : (
        <Table<DongBaoCaoSangKien>
          rowKey="maHoSo"
          size="middle"
          loading={truyVan.isLoading}
          dataSource={duLieu as DongBaoCaoSangKien[]}
          scroll={{ x: 1200 }}
          columns={[
            { title: 'Mã hồ sơ', dataIndex: 'maHoSo', width: 150 },
            { title: 'Tên sáng kiến', dataIndex: 'tenSangKien' },
            { title: 'Tác giả', dataIndex: 'tacGia', width: 200 },
            { title: 'Đơn vị', dataIndex: 'tenDonVi', width: 220, responsive: ['lg'] },
            { title: 'Lĩnh vực', dataIndex: 'tenLinhVuc', width: 170, responsive: ['xl'] },
            {
              title: 'Điểm',
              dataIndex: 'tongDiem',
              width: 90,
              align: 'right',
              sorter: (a, b) => (a.tongDiem ?? 0) - (b.tongDiem ?? 0),
              render: (v: number | null) => v?.toFixed(2) ?? '—',
            },
            { title: 'Mức công nhận', dataIndex: 'tenMucCongNhan', width: 190 },
            ...(loai === 'sang-kien-chua-dat'
              ? [{ title: 'Lý do', dataIndex: 'lyDo', width: 260 }]
              : [
                  {
                    title: 'Ngày công nhận',
                    dataIndex: 'ngayCongNhan',
                    width: 150,
                    render: (v: string | null) => ngayGio(v, false),
                  },
                  { title: 'Số quyết định', dataIndex: 'soQuyetDinh', width: 160 },
                ]),
          ]}
          pagination={{ pageSize: 50, showSizeChanger: true, showTotal: (t) => `Tổng ${t} hồ sơ` }}
        />
      )}
    </Card>
  );
}
