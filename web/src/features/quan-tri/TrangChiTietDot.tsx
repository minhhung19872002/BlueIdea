import { useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import {
  Alert,
  Button,
  Card,
  Col,
  Descriptions,
  Progress,
  Row,
  Space,
  Statistic,
  Table,
  Tabs,
  Tag,
  Typography,
} from 'antd';
import { FileExcelOutlined } from '@ant-design/icons';
import { useQuery } from '@tanstack/react-query';

import { taiTep } from '@/api/client';
import {
  apiDotDeNghi,
  apiHoiDong,
  apiQuyetDinh,
  apiSangKien,
  type DanhMucDto,
  type QuyetDinh,
} from '@/api/endpoints';
import { BangSangKien } from '@/features/sang-kien/BangSangKien';
import { KhoiDangTai, KhoiLoi, KhoiRong, ngayGio } from '@/components/ThanhPhanChung';

const TEN_TRANG_THAI_DOT: Record<string, { ten: string; mau: string }> = {
  NHAP: { ten: 'Nháp', mau: 'default' },
  DANG_MO: { ten: 'Đang mở', mau: 'success' },
  DA_DONG: { ten: 'Đã đóng', mau: 'warning' },
  DA_KHOA: { ten: 'Đã khoá', mau: 'error' },
};

const TEN_CAP: Record<string, string> = {
  CO_SO: 'Cấp cơ sở',
  THANH_PHO: 'Cấp thành phố',
  TINH: 'Cấp tỉnh',
};

/**
 * Chức năng 3 — Màn hình chi tiết một đợt đề nghị.
 *
 * Gom mọi thứ thuộc về đợt vào một chỗ: trước đây muốn biết đợt này có bao nhiêu hồ sơ, hội đồng
 * nào phụ trách, đã ra quyết định chưa thì phải mở lần lượt bốn màn hình khác nhau và tự đối chiếu.
 */
export default function TrangChiTietDot() {
  const { id = '' } = useParams<{ id: string }>();
  const [trang, setTrang] = useState(1);
  const [soDong, setSoDong] = useState(20);
  const [sapXep, setSapXep] = useState<string | undefined>();
  const [huong, setHuong] = useState<'asc' | 'desc' | undefined>();

  const { data, isLoading, error, refetch } = useQuery({
    queryKey: ['dot-tong-quan', id],
    queryFn: () => apiDotDeNghi.tongQuan(id),
    enabled: !!id,
  });

  const hoSo = useQuery({
    queryKey: ['dot-ho-so', id, trang, soDong, sapXep, huong],
    queryFn: () => apiSangKien.danhSach({ dotDeNghiId: id, trang, soDong, sapXep, huong }),
    enabled: !!id,
  });

  const hoiDong = useQuery({
    queryKey: ['dot-hoi-dong', id],
    queryFn: () => apiHoiDong.danhSach({ soDong: 100 }),
    enabled: !!id,
  });

  const quyetDinh = useQuery({
    queryKey: ['dot-quyet-dinh', id],
    queryFn: () => apiQuyetDinh.danhSach({ dotDeNghiId: id, soDong: 100 }),
    enabled: !!id,
  });

  if (isLoading) return <KhoiDangTai />;
  if (error) return <KhoiLoi loi={error} thuLai={refetch} />;
  if (!data) return <KhoiRong moTa="Không tìm thấy đợt đề nghị." />;

  const trangThai = TEN_TRANG_THAI_DOT[data.trangThaiDot] ?? {
    ten: data.trangThaiDot,
    mau: 'default',
  };

  const daXong = data.soDat + data.soKhongDat;
  const tienDoXet = data.tongHoSo === 0 ? 0 : Math.round((daXong / data.tongHoSo) * 100);
  const tongPhieu = data.soPhieuDaGui + data.soPhieuCanCham;
  const tienDoCham = tongPhieu === 0 ? 0 : Math.round((data.soPhieuDaGui / tongPhieu) * 100);

  // Hội đồng gắn với đợt này; danh sách hội đồng trả về toàn bộ nên lọc ở đây.
  const hoiDongCuaDot = (hoiDong.data?.duLieu ?? []).filter(
    (x) => (x as DanhMucDto & { dotDeNghiId?: string | null }).dotDeNghiId === id,
  );

  return (
    <Card
      title={
        <Space wrap>
          <Typography.Text strong>{data.ma}</Typography.Text>
          <span>{data.ten}</span>
          <Tag color={trangThai.mau}>{trangThai.ten}</Tag>
        </Space>
      }
      extra={
        <Space>
          <Link to="/quan-tri/danh-muc/dot-de-nghi">
            <Button>Danh sách đợt</Button>
          </Link>
          <Button
            icon={<FileExcelOutlined />}
            onClick={() =>
              taiTep('/api/v1/sang-kien/xuat-excel', `ho-so-${data.ma}.xlsx`, {
                dotDeNghiId: id,
              })
            }
          >
            Xuất hồ sơ
          </Button>
        </Space>
      }
    >
      <Row gutter={[12, 12]} style={{ marginBottom: 16 }}>
        <Col xs={12} md={6}>
          <Card size="small">
            <Statistic title="Tổng hồ sơ" value={data.tongHoSo} />
          </Card>
        </Col>
        <Col xs={12} md={6}>
          <Card size="small">
            <Statistic title="Đang xử lý" value={data.soDangXuLy} valueStyle={{ color: '#1677ff' }} />
          </Card>
        </Col>
        <Col xs={12} md={6}>
          <Card size="small">
            <Statistic title="Được công nhận" value={data.soDat} valueStyle={{ color: '#389e0d' }} />
          </Card>
        </Col>
        <Col xs={12} md={6}>
          <Card size="small">
            <Statistic title="Không đạt" value={data.soKhongDat} valueStyle={{ color: '#cf1322' }} />
          </Card>
        </Col>
      </Row>

      <Tabs
        items={[
          {
            key: 'thong-tin',
            label: 'Thông tin chung',
            children: (
              <Descriptions bordered size="small" column={{ xs: 1, md: 2 }}>
                <Descriptions.Item label="Mã đợt">{data.ma}</Descriptions.Item>
                <Descriptions.Item label="Tên đợt">{data.ten}</Descriptions.Item>
                <Descriptions.Item label="Năm">{data.nam}</Descriptions.Item>
                <Descriptions.Item label="Cấp xét duyệt">
                  {TEN_CAP[data.capXetDuyet] ?? data.capXetDuyet}
                </Descriptions.Item>
                <Descriptions.Item label="Trạng thái">
                  <Tag color={trangThai.mau}>{trangThai.ten}</Tag>
                </Descriptions.Item>
                <Descriptions.Item label="Hạn nộp hồ sơ">
                  {ngayGio(data.hanNopHoSo)}
                </Descriptions.Item>
                <Descriptions.Item label="Hạn chấm điểm">
                  {ngayGio(data.hanChamDiem)}
                </Descriptions.Item>
              </Descriptions>
            ),
          },
          {
            key: 'don-vi',
            label: `Đơn vị áp dụng (${data.donViApDung.length})`,
            children:
              data.donViApDung.length === 0 ? (
                <Alert
                  type="info"
                  showIcon
                  message="Đợt này áp dụng cho toàn bộ đơn vị."
                  description="Không khai báo đơn vị nào nghĩa là mọi đơn vị đều nộp được hồ sơ vào đợt."
                />
              ) : (
                <Table<DanhMucDto>
                  rowKey="id"
                  size="small"
                  dataSource={data.donViApDung}
                  pagination={false}
                  columns={[
                    { title: 'Mã', dataIndex: 'ma', width: 200 },
                    { title: 'Tên đơn vị', dataIndex: 'ten' },
                  ]}
                />
              ),
          },
          {
            key: 'quy-trinh',
            label: 'Quy trình & tiêu chí',
            children: (
              <Descriptions bordered size="small" column={1}>
                <Descriptions.Item label="Quy trình xử lý">
                  {data.tenQuyTrinh ?? (
                    <Typography.Text type="warning">
                      Chưa gắn quy trình — hồ sơ nộp vào đợt sẽ không chạy được luồng xử lý.
                    </Typography.Text>
                  )}
                </Descriptions.Item>
                <Descriptions.Item label="Bộ tiêu chí chấm">
                  {data.tenBoTieuChi ?? (
                    <Typography.Text type="warning">
                      Chưa gắn bộ tiêu chí — hội đồng chưa chấm điểm được.
                    </Typography.Text>
                  )}
                </Descriptions.Item>
              </Descriptions>
            ),
          },
          {
            key: 'ho-so',
            label: `Hồ sơ (${data.tongHoSo})`,
            children: (
              <BangSangKien
                duLieu={hoSo.data?.duLieu ?? []}
                tongSo={hoSo.data?.tongSo ?? 0}
                trang={trang}
                soDong={soDong}
                dangTai={hoSo.isLoading}
                thamSoXuat={{ dotDeNghiId: id }}
                tenTepXuat={`ho-so-${data.ma}.xlsx`}
                chonCot
                onDoiTrang={(t, s) => {
                  setTrang(t);
                  setSoDong(s);
                }}
                onDoiSapXep={(cot, chieu) => {
                  setSapXep(cot);
                  setHuong(chieu);
                  setTrang(1);
                }}
              />
            ),
          },
          {
            key: 'hoi-dong',
            label: `Hội đồng & chấm điểm (${data.soHoiDong})`,
            children: (
              <>
                <Row gutter={[12, 12]} style={{ marginBottom: 16 }}>
                  <Col xs={24} md={12}>
                    <Card size="small" title="Tiến độ xét duyệt">
                      <Progress percent={tienDoXet} status={tienDoXet === 100 ? 'success' : 'active'} />
                      <Typography.Text type="secondary" style={{ fontSize: 12 }}>
                        {daXong}/{data.tongHoSo} hồ sơ đã có kết quả
                      </Typography.Text>
                    </Card>
                  </Col>
                  <Col xs={24} md={12}>
                    <Card size="small" title="Tiến độ chấm điểm">
                      <Progress
                        percent={tienDoCham}
                        status={tienDoCham === 100 ? 'success' : 'active'}
                      />
                      <Typography.Text type="secondary" style={{ fontSize: 12 }}>
                        {data.soPhieuDaGui}/{tongPhieu} phiếu đã gửi
                        {data.soPhieuCanCham > 0 && ` — còn ${data.soPhieuCanCham} phiếu chưa chấm`}
                      </Typography.Text>
                    </Card>
                  </Col>
                </Row>

                <Table<DanhMucDto>
                  rowKey="id"
                  size="small"
                  loading={hoiDong.isLoading}
                  dataSource={hoiDongCuaDot}
                  pagination={false}
                  locale={{ emptyText: <KhoiRong moTa="Chưa có hội đồng nào gắn với đợt này." /> }}
                  columns={[
                    { title: 'Mã', dataIndex: 'ma', width: 180 },
                    {
                      title: 'Tên hội đồng',
                      dataIndex: 'ten',
                      render: (v: string, dong) => <Link to={`/hoi-dong/${dong.id}`}>{v}</Link>,
                    },
                    {
                      title: 'Trạng thái',
                      dataIndex: 'trangThai',
                      width: 130,
                      render: (v: number) =>
                        v === 1 ? <Tag color="success">Hoạt động</Tag> : <Tag>Ngừng</Tag>,
                    },
                  ]}
                />
              </>
            ),
          },
          {
            key: 'quyet-dinh',
            label: `Quyết định (${data.soQuyetDinh})`,
            children: (
              <Table<QuyetDinh>
                rowKey="id"
                size="small"
                loading={quyetDinh.isLoading}
                dataSource={quyetDinh.data?.duLieu ?? []}
                pagination={false}
                scroll={{ x: 900 }}
                locale={{
                  emptyText: <KhoiRong moTa="Đợt này chưa ban hành quyết định công nhận nào." />,
                }}
                columns={[
                  {
                    title: 'Số quyết định',
                    dataIndex: 'soQuyetDinh',
                    width: 200,
                    render: (v: string) => <Link to="/quyet-dinh">{v}</Link>,
                  },
                  { title: 'Trích yếu', dataIndex: 'trichYeu', ellipsis: true },
                  {
                    title: 'Ngày ban hành',
                    dataIndex: 'ngayBanHanh',
                    width: 150,
                    render: (v: string | null) => ngayGio(v, false),
                  },
                  { title: 'Người ký', dataIndex: 'nguoiKy', width: 180 },
                  {
                    title: 'Số sáng kiến',
                    dataIndex: 'soSangKien',
                    width: 130,
                    align: 'right',
                  },
                  {
                    title: 'Đã công bố',
                    dataIndex: 'soDaCongBo',
                    width: 130,
                    align: 'right',
                  },
                  {
                    title: 'Ký số',
                    dataIndex: 'daKySo',
                    width: 110,
                    render: (v: boolean) =>
                      v ? <Tag color="success">Đã ký</Tag> : <Tag>Chưa ký</Tag>,
                  },
                ]}
              />
            ),
          },
        ]}
      />
    </Card>
  );
}
