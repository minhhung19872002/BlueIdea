import { useState } from 'react';
import { useSearchParams } from 'react-router-dom';
import {
  App,
  Button,
  Card,
  Col,
  Empty,
  Row,
  Select,
  Space,
  Table,
  Tag,
  Typography,
} from 'antd';
import { FileExcelOutlined, FilePdfOutlined, PlayCircleOutlined } from '@ant-design/icons';
import { useQuery } from '@tanstack/react-query';

import { LoiApi, taiTep } from '@/api/client';
import {
  apiDonVi,
  apiDotDeNghi,
  apiLinhVuc,
  apiNhapXuat,
  taoApiDanhMuc,
  type KetQuaBaoCaoTuyBien,
} from '@/api/endpoints';
import { KhoiLoi, KhoiRong } from '@/components/ThanhPhanChung';
import { DaiTabTrang } from '@/components/DaiTabTrang';

const apiBieuMauThongKe = taoApiDanhMuc('/api/v1/danh-muc/bieu-mau-thong-ke');

/** Các nhánh con của trang — thiết kế gộp thành một mục ở thanh điều hướng. */
const DS_TAB = [
  { ma: 'sang-kien-dat', ten: 'Sáng kiến đạt', duongDan: '/bao-cao/sang-kien-dat' },
  { ma: 'sang-kien-chua-dat', ten: 'Chưa đạt', duongDan: '/bao-cao/sang-kien-chua-dat' },
  { ma: 'theo-don-vi', ten: 'Theo đơn vị', duongDan: '/bao-cao/theo-don-vi' },
  { ma: 'ket-qua', ten: 'Kết quả sáng kiến', duongDan: '/bao-cao/ket-qua' },
  { ma: 'tuy-bien', ten: 'Tuỳ biến', duongDan: '/bao-cao/tuy-bien' },
];

/** Chức năng 7 — Báo cáo tuỳ biến sinh động từ cấu hình biểu mẫu thống kê. */
export default function TrangBaoCaoTuyBien() {
  const { message } = App.useApp();

  /*
   * Nhận sẵn biểu mẫu qua địa chỉ: màn hình quản trị Biểu mẫu thống kê có nút "chạy thử" trỏ
   * sang đây, mở ra mà phải tự tìm lại đúng biểu mẫu vừa sửa trong ô chọn thì nút đó vô nghĩa.
   */
  const [thamSoUrl] = useSearchParams();
  const [bieuMauId, setBieuMauId] = useState<string | undefined>(
    thamSoUrl.get('bieuMauId') ?? undefined,
  );
  const [dotDeNghiId, setDotDeNghiId] = useState<string | undefined>();
  const [donViId, setDonViId] = useState<string | undefined>();
  const [linhVucId, setLinhVucId] = useState<string | undefined>();
  const [ketQua, setKetQua] = useState<string | undefined>();
  const [daChay, setDaChay] = useState(false);

  const { data: cacBieuMau, error: loiBieuMau, refetch } = useQuery({
    queryKey: ['bieu-mau-thong-ke-chon'],
    queryFn: apiBieuMauThongKe.chon,
  });

  const { data: cacDot } = useQuery({ queryKey: ['dot-chon'], queryFn: apiDotDeNghi.chon });
  const { data: cacDonVi } = useQuery({ queryKey: ['don-vi-chon'], queryFn: apiDonVi.chon });
  const { data: cacLinhVuc } = useQuery({ queryKey: ['linh-vuc-chon'], queryFn: apiLinhVuc.chon });

  const thamSo = { dotDeNghiId, donViId, linhVucId, ketQua };

  const {
    data: baoCao,
    isFetching,
    error: loiChay,
  } = useQuery<KetQuaBaoCaoTuyBien>({
    queryKey: ['bao-cao-tuy-bien', bieuMauId, thamSo],
    queryFn: () => apiNhapXuat.chayBaoCaoTuyBien(bieuMauId!, thamSo),
    enabled: !!bieuMauId && daChay,
  });

  if (loiBieuMau) return <KhoiLoi loi={loiBieuMau} thuLai={refetch} />;

  /*
   * Chỉ hiện nút của định dạng mà biểu mẫu cho phép.
   *
   * Máy chủ vẫn chặn lần nữa khi nhận yêu cầu — nhưng để nút hiện rồi báo lỗi khi bấm thì người
   * dùng không hiểu vì sao mình bị từ chối một việc mà giao diện vừa mời làm.
   *
   * Biểu mẫu chưa khai gì thì máy chủ trả về cả hai, nên không màn hình nào mất nút.
   */
  const choPhep = baoCao?.dinhDangXuat ?? [];
  const coDuLieu = !!baoCao && baoCao.cacDong.length > 0;

  async function xuat(dinhDang: 'XLSX' | 'PDF') {
    if (!bieuMauId) return;

    const duongDan =
      dinhDang === 'PDF'
        ? apiNhapXuat.duongDanXuatBaoCaoTuyBienPdf(bieuMauId)
        : apiNhapXuat.duongDanXuatBaoCaoTuyBien(bieuMauId);

    try {
      await taiTep(
        duongDan,
        `${baoCao?.tenBaoCao ?? 'bao-cao'}.${dinhDang.toLowerCase()}`,
        thamSo,
      );
    } catch (loi) {
      message.error(loi instanceof LoiApi ? loi.message : 'Không xuất được tệp.');
    }
  }

  return (
    <Card
      title="Thống kê tổng hợp"
      extra={
        <Space>
          <Button
            type="primary"
            icon={<PlayCircleOutlined />}
            disabled={!bieuMauId}
            loading={isFetching}
            onClick={() => setDaChay(true)}
          >
            Chạy báo cáo
          </Button>
          {choPhep.includes('XLSX') && (
            <Button
              icon={<FileExcelOutlined />}
              disabled={!coDuLieu}
              onClick={() => void xuat('XLSX')}
            >
              Xuất Excel
            </Button>
          )}
          {choPhep.includes('PDF') && (
            <Button
              icon={<FilePdfOutlined />}
              disabled={!coDuLieu}
              onClick={() => void xuat('PDF')}
            >
              Xuất PDF
            </Button>
          )}
        </Space>
      }
    >
      <DaiTabTrang danhSach={DS_TAB} dangChon={'tuy-bien'} />

      <Row gutter={[12, 12]} style={{ marginBottom: 16 }}>
        <Col xs={24} md={8}>
          <Typography.Text type="secondary" style={{ fontSize: 12 }}>
            Biểu mẫu thống kê
          </Typography.Text>
          <Select
            style={{ width: '100%' }}
            placeholder="Chọn biểu mẫu đã cấu hình"
            showSearch
            optionFilterProp="label"
            value={bieuMauId}
            options={(cacBieuMau ?? []).map((x) => ({ value: x.id, label: x.ten }))}
            onChange={(v: string) => {
              setBieuMauId(v);
              setDaChay(false);
            }}
          />
        </Col>
        <Col xs={24} md={4}>
          <Typography.Text type="secondary" style={{ fontSize: 12 }}>
            Đợt đề nghị
          </Typography.Text>
          <Select
            style={{ width: '100%' }}
            placeholder="Tất cả"
            allowClear
            showSearch
            optionFilterProp="label"
            value={dotDeNghiId}
            options={(cacDot ?? []).map((x) => ({ value: x.id, label: x.ten }))}
            onChange={setDotDeNghiId}
          />
        </Col>
        <Col xs={24} md={4}>
          <Typography.Text type="secondary" style={{ fontSize: 12 }}>
            Đơn vị
          </Typography.Text>
          <Select
            style={{ width: '100%' }}
            placeholder="Tất cả"
            allowClear
            showSearch
            optionFilterProp="label"
            value={donViId}
            options={(cacDonVi ?? []).map((x) => ({ value: x.id, label: x.ten }))}
            onChange={setDonViId}
          />
        </Col>
        <Col xs={24} md={4}>
          <Typography.Text type="secondary" style={{ fontSize: 12 }}>
            Lĩnh vực
          </Typography.Text>
          <Select
            style={{ width: '100%' }}
            placeholder="Tất cả"
            allowClear
            showSearch
            optionFilterProp="label"
            value={linhVucId}
            options={(cacLinhVuc ?? []).map((x) => ({ value: x.id, label: x.ten }))}
            onChange={setLinhVucId}
          />
        </Col>
        <Col xs={24} md={4}>
          <Typography.Text type="secondary" style={{ fontSize: 12 }}>
            Kết quả
          </Typography.Text>
          <Select
            style={{ width: '100%' }}
            placeholder="Tất cả"
            allowClear
            value={ketQua}
            options={[
              { value: 'DAT', label: 'Đạt' },
              { value: 'KHONG_DAT', label: 'Không đạt' },
            ]}
            onChange={setKetQua}
          />
        </Col>
      </Row>

      {loiChay && <KhoiLoi loi={loiChay} />}

      {!daChay && !loiChay && (
        <KhoiRong moTa="Chọn một biểu mẫu thống kê rồi bấm “Chạy báo cáo”. Cột và cách tổng hợp lấy từ cấu hình biểu mẫu, không cố định trong mã nguồn." />
      )}

      {daChay && baoCao && !loiChay && (
        <>
          <Space wrap style={{ marginBottom: 12 }}>
            <Typography.Text strong>{baoCao.tenBaoCao}</Typography.Text>
            <Tag color="blue">{baoCao.tongSo} bản ghi</Tag>
            {Object.entries(baoCao.boLocDaApDung).map(([khoa, giaTri]) => (
              <Tag key={khoa}>
                {khoa}: {giaTri}
              </Tag>
            ))}
          </Space>

          <Table<string[]>
            rowKey={(_dong, chiMuc) => String(chiMuc)}
            size="small"
            loading={isFetching}
            dataSource={baoCao.cacDong}
            scroll={{ x: Math.max(600, baoCao.tieuDeCot.length * 160) }}
            pagination={{ pageSize: 25, showSizeChanger: true }}
            locale={{ emptyText: <Empty description="Không có dữ liệu khớp bộ lọc" /> }}
            columns={baoCao.tieuDeCot.map((tieuDe, chiMuc) => ({
              title: tieuDe,
              key: String(chiMuc),
              // Dòng tổng cộng (dòng cuối) in đậm để phân biệt với dữ liệu.
              render: (_v: unknown, dong: string[], viTri: number) =>
                viTri === baoCao.cacDong.length - 1 && dong.includes('TỔNG CỘNG') ? (
                  <strong>{dong[chiMuc]}</strong>
                ) : (
                  dong[chiMuc]
                ),
            }))}
          />
        </>
      )}
    </Card>
  );
}
