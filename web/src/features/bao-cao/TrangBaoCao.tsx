import { useMemo, useState } from 'react';
import { useParams } from 'react-router-dom';
import { Button, Card, Col, Row, Select, Space, Statistic, Table, Typography } from 'antd';
import { FileExcelOutlined, FilePdfOutlined } from '@ant-design/icons';
import { useQuery } from '@tanstack/react-query';

import { taiTep } from '@/api/client';
import {
  apiBaoCao,
  apiDonVi,
  apiDotDeNghi,
  apiLinhVuc,
  type BaoCaoTongHopNam,
  type DongBaoCaoDonVi,
  type DongBaoCaoSangKien,
  type DongBaoCaoTacGia,
  type DongThoiGianXuLy,
  type MucThongKe,
} from '@/api/endpoints';
import {
  KhoiDangTai,
  KhoiKhongNhanRa,
  KhoiLoi,
  KhoiRong,
  ngayGio,
} from '@/components/ThanhPhanChung';
import { DaiTabTrang } from '@/components/DaiTabTrang';

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
  'theo-tac-gia': {
    tieuDe: 'Thống kê sáng kiến theo tác giả',
    duongDanXuat: '/api/v1/bao-cao/theo-tac-gia/xuat-excel',
    tenTep: 'thong-ke-theo-tac-gia.xlsx',
  },
  'thoi-gian-xu-ly': {
    tieuDe: 'Thời gian xử lý trung bình theo bước',
    duongDanXuat: '/api/v1/bao-cao/thoi-gian-xu-ly/xuat-excel',
    tenTep: 'thoi-gian-xu-ly.xlsx',
  },
  'tong-hop-nam': {
    tieuDe: 'Báo cáo tổng hợp năm',
    duongDanXuat: '/api/v1/bao-cao/theo-don-vi/xuat-excel',
    tenTep: 'bao-cao-tong-hop-nam.xlsx',
  },
};

const NAM_HIEN_TAI = new Date().getFullYear();
const DS_NAM = [NAM_HIEN_TAI + 1, NAM_HIEN_TAI, NAM_HIEN_TAI - 1, NAM_HIEN_TAI - 2];

/** Các nhánh con của trang — thiết kế gộp thành một mục ở thanh điều hướng. */
const DS_TAB = [
  { ma: 'sang-kien-dat', ten: 'Sáng kiến đạt', duongDan: '/bao-cao/sang-kien-dat' },
  { ma: 'sang-kien-chua-dat', ten: 'Chưa đạt', duongDan: '/bao-cao/sang-kien-chua-dat' },
  { ma: 'theo-don-vi', ten: 'Theo đơn vị', duongDan: '/bao-cao/theo-don-vi' },
  { ma: 'ket-qua', ten: 'Kết quả sáng kiến', duongDan: '/bao-cao/ket-qua' },
  { ma: 'theo-tac-gia', ten: 'Theo tác giả', duongDan: '/bao-cao/theo-tac-gia' },
  { ma: 'thoi-gian-xu-ly', ten: 'Thời gian xử lý', duongDan: '/bao-cao/thoi-gian-xu-ly' },
  { ma: 'tong-hop-nam', ten: 'Tổng hợp năm', duongDan: '/bao-cao/tong-hop-nam' },
  { ma: 'tuy-bien', ten: 'Tuỳ biến', duongDan: '/bao-cao/tuy-bien' },
];

/** Chức năng 38–40 — Các báo cáo bắt buộc. */
export default function TrangBaoCao() {
  const { loai = 'sang-kien-dat' } = useParams<{ loai: string }>();
  /*
   * Loại báo cáo lạ thì báo hẳn ra ở cuối hàm chứ không lặng lẽ hiện "Sáng kiến được công nhận".
   * Hiện nhầm báo cáo nguy hiểm hơn báo lỗi: người dùng đem số liệu đó đi họp mà không biết mình
   * đang xem báo cáo khác.
   *
   * Vẫn lấy cấu hình mặc định để các hook bên dưới chạy đủ: thoát sớm ở đây thì lần đổi loại từ
   * hợp lệ sang không hợp lệ sẽ khiến React dựng ít hook hơn lần trước và vỡ.
   */
  const nhanRa = Object.prototype.hasOwnProperty.call(CAU_HINH_BAO_CAO, loai);
  const cauHinh = CAU_HINH_BAO_CAO[loai] ?? CAU_HINH_BAO_CAO['sang-kien-dat'];

  const [dotDeNghiId, setDotDeNghiId] = useState<string | undefined>();
  const [linhVucId, setLinhVucId] = useState<string | undefined>();
  const [donViId, setDonViId] = useState<string | undefined>();
  const [nam, setNam] = useState<number>(new Date().getFullYear());

  // Nam ap dung cho MOI bao cao (truoc day tham so nam bi may chu bo qua, chon xong so lieu
  // khong doi); rieng bao cao tong hop nam thi nam la truc chinh cua bao cao.
  const thamSo = useMemo(
    () => ({ dotDeNghiId, linhVucId, donViId, nam }),
    [dotDeNghiId, linhVucId, donViId, nam],
  );

  const { data: cacDot } = useQuery({ queryKey: ['dot-chon'], queryFn: apiDotDeNghi.chon });
  const { data: cacLinhVuc } = useQuery({ queryKey: ['linh-vuc-chon'], queryFn: apiLinhVuc.chon });
  const { data: cacDonVi } = useQuery({ queryKey: ['don-vi-chon'], queryFn: apiDonVi.chon });

  const truyVan = useQuery<
    Array<DongBaoCaoSangKien | DongBaoCaoDonVi | DongBaoCaoTacGia | DongThoiGianXuLy>
  >({
    queryKey: ['bao-cao', loai, thamSo],
    enabled: nhanRa && loai !== 'tong-hop-nam',
    queryFn: () => {
      if (loai === 'theo-don-vi') return apiBaoCao.theoDonVi(thamSo);
      if (loai === 'theo-tac-gia') return apiBaoCao.theoTacGia(thamSo);
      if (loai === 'thoi-gian-xu-ly') return apiBaoCao.thoiGianXuLy(thamSo);
      if (loai === 'sang-kien-chua-dat') return apiBaoCao.sangKienChuaDat(thamSo);
      return apiBaoCao.sangKienDat(thamSo);
    },
  });

  const tongHopNam = useQuery({
    queryKey: ['bao-cao-tong-hop-nam', nam, dotDeNghiId, linhVucId, donViId],
    enabled: nhanRa && loai === 'tong-hop-nam',
    queryFn: () => apiBaoCao.tongHopNam(nam, { dotDeNghiId, linhVucId, donViId }),
  });

  if (!nhanRa) {
    return (
      <KhoiKhongNhanRa
        ma={loai}
        hopLe={Object.entries(CAU_HINH_BAO_CAO).map(([k, v]) => ({ ma: k, ten: v.tieuDe }))}
        duongDanGoc="/bao-cao"
      />
    );
  }

  if (truyVan.error) return <KhoiLoi loi={truyVan.error} thuLai={truyVan.refetch} />;
  if (tongHopNam.error) {
    return <KhoiLoi loi={tongHopNam.error} thuLai={tongHopNam.refetch} />;
  }

  const duLieu = truyVan.data ?? [];

  return (
    <Card
      title="Thống kê tổng hợp"
      extra={
        <Space className="khong-in">
          {loai !== 'tong-hop-nam' && (
            <Button
              icon={<FileExcelOutlined />}
              onClick={() => taiTep(cauHinh.duongDanXuat, cauHinh.tenTep, thamSo)}
            >
              Xuất Excel
            </Button>
          )}
          {loai === 'tong-hop-nam' ? (
            <Button
              icon={<FilePdfOutlined />}
              onClick={() =>
                taiTep(
                  apiBaoCao.duongDanTongHopNamPdf(nam),
                  `bao-cao-tong-hop-nam-${nam}.pdf`,
                  { dotDeNghiId, linhVucId, donViId },
                )
              }
            >
              Xuất PDF
            </Button>
          ) : (
            !cauHinh.laTheoDonVi && (
              <Button
                icon={<FilePdfOutlined />}
                onClick={() =>
                  taiTep('/api/v1/bao-cao/sang-kien-dat/xuat-pdf', 'bao-cao.pdf', thamSo)
                }
              >
                Xuất PDF
              </Button>
            )
          )}
          <Button onClick={() => window.print()}>In</Button>
        </Space>
      }
    >
      <DaiTabTrang danhSach={DS_TAB} dangChon={loai} />

      <Row gutter={[8, 8]} style={{ marginBottom: 12 }} className="khong-in">
        <Col xs={24} md={6}>
          <Select
            style={{ width: '100%' }}
            value={nam}
            options={DS_NAM.map((x) => ({ value: x, label: `Năm ${x}` }))}
            onChange={setNam}
          />
        </Col>
        <Col xs={24} md={6}>
          <Select
            style={{ width: '100%' }}
            placeholder="Tất cả đợt"
            allowClear
            value={dotDeNghiId}
            options={(cacDot ?? []).map((x) => ({ value: x.id, label: x.ten }))}
            onChange={setDotDeNghiId}
          />
        </Col>
        <Col xs={24} md={6}>
          <Select
            style={{ width: '100%' }}
            placeholder="Tất cả lĩnh vực"
            allowClear
            value={linhVucId}
            options={(cacLinhVuc ?? []).map((x) => ({ value: x.id, label: x.ten }))}
            onChange={setLinhVucId}
          />
        </Col>
        <Col xs={24} md={6}>
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

      {loai === 'tong-hop-nam' ? (
        <BangTongHopNam duLieu={tongHopNam.data} dangTai={tongHopNam.isLoading} />
      ) : loai === 'theo-tac-gia' ? (
        <Table<DongBaoCaoTacGia>
          rowKey={(x) => `${x.hoTen}-${x.donViCongTac ?? ''}`}
          size="middle"
          loading={truyVan.isLoading}
          dataSource={duLieu as DongBaoCaoTacGia[]}
          scroll={{ x: 1000 }}
          locale={{ emptyText: <KhoiRong moTa="Chưa có dữ liệu phù hợp với bộ lọc." /> }}
          columns={[
            { title: 'Họ và tên', dataIndex: 'hoTen', width: 220 },
            { title: 'Đơn vị công tác', dataIndex: 'donViCongTac', width: 240, ellipsis: true },
            { title: 'Chức vụ', dataIndex: 'chucVu', width: 180, responsive: ['xl'] },
            {
              title: 'Tổng số',
              dataIndex: 'tongSo',
              width: 110,
              align: 'right',
              sorter: (a, b) => a.tongSo - b.tongSo,
            },
            { title: 'Tác giả chính', dataIndex: 'soLaTacGiaChinh', width: 140, align: 'right' },
            {
              title: 'Đạt',
              dataIndex: 'soDat',
              width: 90,
              align: 'right',
              sorter: (a, b) => a.soDat - b.soDat,
            },
            {
              title: 'Điểm TB',
              dataIndex: 'diemTrungBinh',
              width: 110,
              align: 'right',
              render: (v: number | null) => v?.toFixed(2) ?? '—',
            },
            {
              title: 'Tỷ lệ đạt',
              dataIndex: 'tyLeDat',
              width: 110,
              align: 'right',
              render: (v: number) => `${v.toFixed(2)}%`,
            },
          ]}
          pagination={{ pageSize: 50, showSizeChanger: true, showTotal: (t) => `${t} tác giả` }}
        />
      ) : loai === 'thoi-gian-xu-ly' ? (
        <Table<DongThoiGianXuLy>
          rowKey="tenBuoc"
          size="middle"
          loading={truyVan.isLoading}
          dataSource={duLieu as DongThoiGianXuLy[]}
          scroll={{ x: 800 }}
          locale={{
            emptyText: (
              <KhoiRong moTa="Chưa có lượt xử lý nào hoàn thành để tính thời gian trung bình." />
            ),
          }}
          columns={[
            { title: 'Bước xử lý', dataIndex: 'tenBuoc' },
            { title: 'Số lượt', dataIndex: 'soLuot', width: 110, align: 'right' },
            {
              title: 'Trung bình (ngày)',
              dataIndex: 'soNgayTrungBinh',
              width: 170,
              align: 'right',
              sorter: (a, b) => a.soNgayTrungBinh - b.soNgayTrungBinh,
              render: (v: number) => v.toFixed(2),
            },
            {
              title: 'Lâu nhất (ngày)',
              dataIndex: 'soNgayLauNhat',
              width: 160,
              align: 'right',
              render: (v: number) => v.toFixed(2),
            },
            {
              title: 'Lượt quá hạn',
              dataIndex: 'soLuotQuaHan',
              width: 140,
              align: 'right',
              render: (v: number) =>
                v > 0 ? <Typography.Text type="danger">{v}</Typography.Text> : v,
            },
          ]}
          pagination={false}
        />
      ) : !truyVan.isLoading && duLieu.length === 0 ? (
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

// ---------------------------------------------------------------------------

function BangTongHopNam({
  duLieu,
  dangTai,
}: {
  duLieu?: BaoCaoTongHopNam;
  dangTai: boolean;
}) {
  if (dangTai) return <KhoiDangTai />;
  if (!duLieu) return <KhoiRong moTa="Chưa có số liệu cho năm đã chọn." />;

  const bang = (tieuDe: string, cacMuc: MucThongKe[]) => (
    <Card size="small" title={tieuDe} style={{ marginBottom: 12 }}>
      {cacMuc.length === 0 ? (
        <Typography.Text type="secondary">Không có dữ liệu.</Typography.Text>
      ) : (
        <Table<MucThongKe>
          rowKey="ten"
          size="small"
          dataSource={cacMuc}
          pagination={false}
          showHeader={false}
          columns={[
            { dataIndex: 'ten' },
            { dataIndex: 'soLuong', width: 90, align: 'right' },
          ]}
        />
      )}
    </Card>
  );

  return (
    <>
      <Row gutter={[12, 12]} style={{ marginBottom: 16 }}>
        <Col xs={12} md={6}>
          <Card size="small">
            <Statistic title="Tổng hồ sơ" value={duLieu.tongHoSo} />
          </Card>
        </Col>
        <Col xs={12} md={6}>
          <Card size="small">
            <Statistic
              title="Được công nhận"
              value={duLieu.soDat}
              suffix={`(${duLieu.tyLeDat.toFixed(2)}%)`}
              valueStyle={{ color: '#389e0d' }}
            />
          </Card>
        </Col>
        <Col xs={12} md={6}>
          <Card size="small">
            <Statistic title="Tác giả tham gia" value={duLieu.soTacGia} />
          </Card>
        </Col>
        <Col xs={12} md={6}>
          <Card size="small">
            <Statistic
              title="Giá trị làm lợi ước tính"
              value={duLieu.tongGiaTriLamLoi ?? 0}
              formatter={(v) => Number(v).toLocaleString('vi-VN')}
              suffix="đ"
            />
          </Card>
        </Col>
      </Row>

      <Row gutter={[12, 12]}>
        <Col xs={24} lg={8}>
          {bang('Theo lĩnh vực', duLieu.theoLinhVuc)}
        </Col>
        <Col xs={24} lg={8}>
          {bang('Theo đợt đề nghị', duLieu.theoDot)}
        </Col>
        <Col xs={24} lg={8}>
          {bang('Theo mức công nhận', duLieu.theoMucCongNhan)}
        </Col>
      </Row>
    </>
  );
}
